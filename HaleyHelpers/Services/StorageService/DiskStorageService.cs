using Haley.Abstractions;
using Haley.Enums;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;
using Haley.Utils;
using System.CodeDom;

namespace Haley.Services {
    public class DiskStorageService : IDiskStorageService {

        public DiskStorageService(string basePath): this(basePath, null) { }

        public DiskStorageService(string basePath, IStorageIndexingService indexer) {
            BasePath = basePath;
            //This is supposedly the directory where all storage goes into.
            if (BasePath == null) {
                BasePath = AssemblyUtils.GetBaseDirectory(parentFolder: "DataStore");
            }
            BasePath = BasePath?.ToLower();
            Indexer = indexer; //Set indexer at the beginning.
        }
        public string BasePath { get; }
        public bool EnableIndexing { get; set; }

        IStorageIndexingService Indexer;

        public string GetBasePath() {
            return BasePath;
        }

        #region Disk Storage Management 

        public async Task<IFeedback> RegisterClient(string name, bool iscontrolled, string password = null) {
            //Password will be stored in the .dss.meta file
            if (string.IsNullOrWhiteSpace(name)) return new Feedback(false, "Name cannot be empty");
            if (string.IsNullOrWhiteSpace(password)) password = "admin";
            var cpath = StorageUtils.GetBasePath(name, iscontrolled);
            var path = Path.Combine(BasePath, cpath.path);

            //Create these folders and then register them.
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path); //Create the directory.
            }

            var signing = RandomUtils.GetString(512);
            var encrypt = RandomUtils.GetString(512);

            Dictionary<string, string> clientKeys = new Dictionary<string, string>();
            clientKeys.Add("password", password);
            clientKeys.Add("signing", signing);
            clientKeys.Add("encrypt", encrypt);

            var keysJson = clientKeys.ToJson();
            var pwdFile = Path.Combine(path, ".dss.meta");
            File.WriteAllText(pwdFile,keysJson );   // Over-Write the keys here.

            Indexer.RegisterClient(name)

        }

        public async Task<IObjectCreateResponse> Upload(IObjectUploadRequest input) {
            ObjectCreateResponse result = new ObjectCreateResponse() {
                Status = false,
                RawName = input.RawName
            };
            try {
                var path = input?.BuildStoragePath(BasePath); //This will also ensure we are not trying to delete something 
                if (string.IsNullOrWhiteSpace(path)) {
                    result.Message = "Unable to generate the final storage path. Please check inputs.";
                    return result;
                }

                if (input.BufferSize < 4096) input.BufferSize = 4096; //Default CopyTo from System.IO has 80KB buffersize. We setit as 4KB for fast storage.

                if (input.FileStream == null) throw new ArgumentException($@"File stream is null. Nothing to save.");
                input.FileStream.Position = 0; //Precaution

                if (!FilePreProcess(result, path, input.ResolveMode)) return result;

                //Either file doesn't exists.. or exists and replace

                if (!result.ObjectExists || input.ResolveMode == ObjectExistsResolveMode.Replace) {
                    //TODO : DEFERRED REPLACEMENT
                    //If the file is currently in use, try for 5 times and then replace. May be easy option would be to store in temporary place and then update a database that a temporary file is created and then later, with some background process check the database and try to replace. This way we dont' have to block the api call or wait for completion.
                    await input.FileStream?.TryReplaceFileAsync(path, input.BufferSize);
                }

                if (!result.ObjectExists) result.Message = "Uploaded."; //For skip also, we will return true (but object will exists)
                result.Status = true;
                result.Size = input.FileStream.Length; //storage size in bytes.

            } catch (Exception ex) {
                result.Message = ex.Message;
                result.Status = false;
            }
            return result;
        }

        public Task<IFileStreamResponse> Download(IObjectReadRequest input, bool auto_search_extension = true) {
            IFileStreamResponse result = new FileStreamResponse() { Status = false, Stream = Stream.Null };
            var path = input?.BuildStoragePath(BasePath); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(result);

            if (!File.Exists(path) && auto_search_extension) {
                //If file extension is not present, then search the targetpath for matching filename and fetch the object (if only one is present).

                if (string.IsNullOrWhiteSpace(Path.GetExtension(path))) {
                    var findName = Path.GetFileNameWithoutExtension(path); //If the extension is not available, obviously it will return only file name. But, what if the file is like 'test.' ends with a period (.). So, we use the GetFileNameWithoutExtension method.

                    //Extension not provided. So, lets to see if we have any matching file.
                    DirectoryInfo dinfo = new DirectoryInfo(Path.GetDirectoryName(path));
                    if (!dinfo.Exists) {
                        result.Message = "The directory doesn't exists.";
                        return Task.FromResult(result);
                    }
                    var matchingFiles = dinfo?.GetFiles()?.Where(p => Path.GetFileNameWithoutExtension(p.Name) == findName).ToList();
                    if (matchingFiles.Count() == 1) {
                        path = matchingFiles.FirstOrDefault().FullName;
                    } else if (matchingFiles.Count() > 1) {
                        //We found mathing items but more than one
                        result.Message = "Multiple matching files found. Please provide a valid extension.";
                        return Task.FromResult(result);
                    }
                }
            }

            if (!File.Exists(path)) {
                result.Message = "File doesn't exist.";
                return Task.FromResult(result);
            }
            result.Status = true;
            result.Extension = Path.GetExtension(path);
            result.Stream = new FileStream(path, FileMode.Open, FileAccess.Read) as Stream;
            return Task.FromResult(result); //Stream is open here.
        }

        public async Task<IFeedback> Delete(IObjectReadRequest input) {
            IFeedback feedback = new Feedback() { Status = false };
            var path = input?.BuildStoragePath(BasePath, readonlyMode:true); //This will also ensure we are not trying to create something 
            if (string.IsNullOrWhiteSpace(path)) {
                feedback.Message = "Unable to generate path from provided inputs.";
                return feedback;
            }
           
            if (!File.Exists(path)) {
                feedback.Message = $@"File does not exists : {path}.";
                return feedback;
            }

            feedback.Status = await path.TryDeleteFile();
            feedback.Message = feedback.Status ? "File deleted" : "Unable to delete the file. Check if it is in use by other process & try again.";
            return feedback;
        }

        public IFeedback Exists(IObjectReadRequest input) {
            var feedback = new Feedback() { Status = false };
            var path = input?.BuildStoragePath(BasePath, readonlyMode:true); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) {
                feedback.Message = "Unable to generate path from provided inputs.";
                return feedback;
            }
            bool isFile = input.StorageRoutes?.Last().IsFile ?? false; //Why any of the flag?
            //If last storageroute has a IsFile flag, then it means, we are trying to figure out a file existence.
            if (isFile) {
                feedback.Status = File.Exists(path);
            } else {
                feedback.Status = Directory.Exists(path);
            }
            if (!feedback.Status) feedback.Message = $@"Does not exists {path}";
            return feedback;
        }

        public long GetSize(IObjectReadRequest input) {
            var path = input?.BuildStoragePath(BasePath); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return 0;
            return new FileInfo(path).Length;
        }

        public Task<IDirectoryInfoResponse> GetDirectoryInfo(IObjectReadRequest input) {
            IDirectoryInfoResponse result = new DirectoryInfoResponse() { Status = false};

            var path = input?.BuildStoragePath(BasePath,readonlyMode:true); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) {
                result.Message = "Unable to generate path.";
                return Task.FromResult(result);
            }

            //It is users' responsibility to send a valid path for checking.
            if (!Directory.Exists(path)) {
                result.Message = "Unable to find the specified path. Please check.";
                return Task.FromResult(result);
            }

            var dinfo = new DirectoryInfo(path);

            result.FoldersList = dinfo.GetDirectories()?.Select(p => p.Name)?.ToList();
            result.FilesList = dinfo.GetFiles()?.Select(p => p.Name)?.ToList();
            return Task.FromResult(result);
        }

        public Task<IObjectCreateResponse> CreateDirectory(IObjectReadRequest input, string rawname) {
            IObjectCreateResponse result = new ObjectCreateResponse() {
                Status = false,
                RawName = rawname
            };
            try {
                var path = input?.BuildStoragePath(BasePath);  //This will also ensure we are not trying to delete something 
                if (string.IsNullOrWhiteSpace(path)) {
                    result.Message = $@"Unable to generate the path. Please check inputs.";
                    return Task.FromResult(result);
                }

                if (Directory.Exists(path)) {
                    result.Status = true;
                    result.Message = $@"Directory already exists.";
                    return Task.FromResult(result);
                }
                if (!(path?.EnsureDirectory() ?? false)) {
                    result.Message = $@"Unable to create the directory. Please check if it is valid.";
                    return Task.FromResult(result);
                }

                result.Status = true;
                result.Message = "Created";
            } catch (Exception ex) {
                result.Status = false;
                result.Message = ex.Message;
            }
            return Task.FromResult(result);
        }

        public async  Task<IFeedback> DeleteDirectory(IObjectReadRequest input, bool recursive) {
            IFeedback feedback = new Feedback() { Status = false };
            var path = input?.BuildStoragePath(BasePath, readonlyMode: true);
            if (string.IsNullOrWhiteSpace(path)) {
                feedback.Message = "Unable to generate path from provided inputs.";
                return feedback;
            }
            //How do we verfiy, if this the final target that we wish to delete?
            //We should not by mistake end up deleting a wrong directory.
            var expectedToDelete = input.StorageRoutes?.Last().Path?.ToLower().Trim();

            if (string.IsNullOrWhiteSpace(expectedToDelete) ||
                (expectedToDelete == "\\" || expectedToDelete == "/") ||
                expectedToDelete.Equals(BasePath.ToLower())) {
                feedback.Message = "Path is not valid for deleting.";
                return feedback;
            }
            if (!Directory.Exists(path)) {
                feedback.Message = $@"Directory does not exists. : {path}.";
                return feedback;
            }
            //Directory.Delete(path, recursive);
            await path?.TryDeleteDirectory();
            feedback.Status = true;
            feedback.Message = "Deleted successfully";
            return feedback;
        }

        #endregion

        #region Helpers
       

        bool FilePreProcess(IObjectCreateResponse result, string filePath, ObjectExistsResolveMode conflict) {

            var targetDir = Path.GetDirectoryName(filePath); //Get only the directory.

            //Should we even try to generate the directory first???
            if (!(targetDir?.EnsureDirectory() ?? false)) {
                result.Message = $@"Unable to ensure storage directory. Please check if it is valid. {targetDir}";
                return false;
            }

            if (!filePath.StartsWith(BasePath)) {
                result.Message = "Not authorized for this folder. Please check the path.";
                return false;
            }

            result.ObjectExists = File.Exists(filePath);
            if (result.ObjectExists) {
                switch (conflict) {
                    case ObjectExistsResolveMode.Skip:
                    result.Status = true;
                    result.Message = "File exists. Skipped";
                    return true; //Skip if it already exists.
                    case ObjectExistsResolveMode.ReturnError:
                    result.Status = false;
                    result.Message = $@"File Exists. Returned Error.";
                    return false;
                    case ObjectExistsResolveMode.Replace:
                    result.Message = "Replace initiated";
                    return true;
                }
            }
            return true;
        }

        public Task<IFeedback> AuthorizeClient(object clientInfo, object clientSecret) {
            IFeedback result = new Feedback();
            result.Status = true;
            result.Message = "No default implementation available. All requests authorized.";
            return Task.FromResult(result);
        }
        #endregion
    }
}
