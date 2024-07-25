using Haley.Abstractions;
using Haley.Enums;
using Haley.Services;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;
using Haley.Utils;

namespace Haley.Services {
    public class DiskStorageService : IObjectStorageService {

        public DiskStorageService(string basePath) {
            BasePath = basePath;
            //This is supposedly the directory where all storage goes into.
            if (BasePath == null) {
                BasePath = AssemblyUtils.GetBaseDirectory(parentFolder: "DataStore");
            }
            BasePath = BasePath?.ToLower();
        }

        public string BasePath { get; }

        #region Disk Storage Management 
        public async Task<ObjectCreateResponse> Upload(IObjectUploadRequest input) {
            ObjectCreateResponse result = new ObjectCreateResponse() {
                Status = false,
                RawName = input.ObjectRawName
            };
            try {
                var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
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
                    using (var fs = File.Create(path)) {
                        await input.FileStream.CopyToAsync(fs, input.BufferSize);
                    }
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

        public Task<StreamResponse> Download(IObjectReadRequest input, bool auto_search_extension = true) {
            StreamResponse result = new StreamResponse() { Status = false, Stream = Stream.Null };
            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(result);

            if (!File.Exists(path) && auto_search_extension) {
                //If file extension is not present, then search the targetpath for matching filename and fetch the object (if only one is present).

                if (string.IsNullOrWhiteSpace(Path.GetExtension(path))) {
                    var findName = Path.GetFileNameWithoutExtension(path);
                    //Extension not provided. So, lets to see if we have any matching file.
                    DirectoryInfo dinfo = new DirectoryInfo(Path.GetDirectoryName(path));
                    var matchingFiles = dinfo?.GetFiles()?.Where(p => Path.GetFileNameWithoutExtension(p.Name) == findName).ToList();
                    if (matchingFiles.Count() == 1) {
                        path = matchingFiles.FirstOrDefault().FullName;
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

        public Task<bool> Delete(IObjectReadRequest input) {
            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(false);
           
            if (File.Exists(path)) {
                File.Delete(path);
            }
            return Task.FromResult(true);
        }

        public bool Exists(IObjectReadRequest input) {
            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) return false;
            return File.Exists(path);
        }

        public long GetSize(IObjectReadRequest input) {
            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return 0;
            return new FileInfo(path).Length;
        }

        public Task<DirectoryInfoResponse> GetInfo(IObjectReadRequest input) {
            DirectoryInfoResponse result = new DirectoryInfoResponse() { Status = false};

            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
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

        public Task<ObjectCreateResponse> CreateDirectory(IObjectReadRequest input, string rawname) {
            ObjectCreateResponse result = new ObjectCreateResponse() {
                Status = false,
                RawName = rawname
            };
            try {
                var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
                if (string.IsNullOrWhiteSpace(path)) {
                    result.Message = "Unable to generate the final storage path. Please check inputs.";
                    return Task.FromResult(result);
                }

                if (Directory.Exists(path)) {
                    result.Message = $@"Directory already exists.";
                    return Task.FromResult(result);
                }
                if (!EnsureDirectory(path)) {
                    result.Message = $@"Unable to ensure storage directory. Please check if it is valid.";
                    return Task.FromResult(result);
                }

                result.Status = true;
            } catch (Exception ex) {
                result.Status = false;
                result.Message = ex.Message;
            }
            return Task.FromResult(result);
        }

        public Task<bool> DeleteDirectory(IObjectReadRequest input, bool recursive) {

            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(false);

            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive);
            }
            return Task.FromResult(true);
        }

        #endregion

        #region Helpers
        bool EnsureDirectory(string target) {
            try {
                if (Directory.Exists(target)) return true;
                bool createFlag = true;
                int tryCount = 0;
                while (createFlag) {
                    try {
                        Directory.CreateDirectory(target);
                        if (Directory.Exists(target)) break;
                    } catch (Exception) {
                        if (tryCount > 3) break;
                    }
                    tryCount++;
                }
                return Directory.Exists(target);
            } catch (Exception) {
                throw;
            }
        }

        bool FilePreProcess(ObjectCreateResponse result, string filePath, ObjectExistsResolveMode conflict) {

            var targetDir = Path.GetDirectoryName(filePath); //Get only the directory.

            //Should we even try to generate the directory first???
            if (!EnsureDirectory(targetDir)) {
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

        string SanitizePath(string input) {
            if (string.IsNullOrEmpty(input)) return input;
            if (input == "/" || input == @"\") input = string.Empty; //We cannot have single '/' as path.
            if (input.StartsWith("/") || input.StartsWith(@"\")) input = input.Substring(1); //We cannot have something start with / as well
            return input;
        }

        string Build(List<StorageRoute> routes) {
            string path = BasePath; //Base path cannot be null. it is mandatory for disk storage.
            //Pull the lastone out.
            if (routes == null && routes.Count < 1) return path; //Direclty create inside the basepath (applicable in few cases);

            for (int i = 0; i < routes.Count; i++) { //the -2 is to ensure we ignore the last part.
                var route = routes[i];
                //If we are at the end, ignore
                string wv = route.Path;
                wv = SanitizePath(wv.Trim());
                if (string.IsNullOrWhiteSpace(wv)) continue;
                path = Path.Combine(path, wv);
                if (i == routes.Count - 1) break; //We are at last index. break out without generating or creating a directory.
                if (!route.CreateIfMissing) {
                    //validate the path.
                    if (!Directory.Exists(path)) throw new ArgumentException($@"Failed to validate the route component : {route.Key ?? route.Path}");
                }
                if (!EnsureDirectory(path)) throw new ArgumentException($@"Unable to create the route component : {route.Key ?? route.Path}");
            }

            return path;
        }

        string GetFinalStoragePath(IObjectReadRequest input) {
            if (input == null || !(input is ObjectReadRequest req)) throw new ArgumentNullException($@"{nameof(IObjectReadRequest)} cannot be null. It has to be of type {nameof(ObjectReadRequest)}");

            req.ObjectFullPath = Build(input.StorageRoutes); 
            //What if, the user provided no value and we end up with only the Basepath.
            if (string.IsNullOrWhiteSpace(req.ObjectFullPath)) throw new ArgumentNullException($@"Unable to generate a full object path for the request");

            //If it doesn't start with base path, we replace as well
            if (!req.ObjectFullPath.StartsWith(BasePath)) throw new ArgumentOutOfRangeException("The generated path is not accessible. Please check the inputs.");

            return req.ObjectFullPath;
        }
        #endregion
    }
}
