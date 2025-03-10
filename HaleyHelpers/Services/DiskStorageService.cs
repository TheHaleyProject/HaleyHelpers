﻿using Haley.Abstractions;
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

        public string GetBasePath() {
            return BasePath;
        }

        #region Disk Storage Management 
        public async Task<IObjectCreateResponse> Upload(IObjectUploadRequest input) {
            ObjectCreateResponse result = new ObjectCreateResponse() {
                Status = false,
                RawName = input.RawName
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

        public Task<IFileStreamResponse> Download(IObjectReadRequest input, bool auto_search_extension = true) {
            IFileStreamResponse result = new FileStreamResponse() { Status = false, Stream = Stream.Null };
            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(result);

            if (!File.Exists(path) && auto_search_extension) {
                //If file extension is not present, then search the targetpath for matching filename and fetch the object (if only one is present).

                if (string.IsNullOrWhiteSpace(Path.GetExtension(path))) {
                    var findName = Path.GetFileNameWithoutExtension(path);
                    //Extension not provided. So, lets to see if we have any matching file.
                    DirectoryInfo dinfo = new DirectoryInfo(Path.GetDirectoryName(path));
                    if (!dinfo.Exists) {
                        result.Message = "File doesn't exists in the given path.";
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

        public Task<IFeedback> Delete(IObjectReadRequest input) {
            IFeedback feedback = new Feedback() { Status = false };
            var path = GetFinalStoragePath(input, forReadOnly:true); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) {
                feedback.Message = "Unable to generate path from provided inputs.";
                return Task.FromResult(feedback);
            }
           
            if (!File.Exists(path)) {
                feedback.Message = $@"File does not exists : {path}.";
                return Task.FromResult(feedback);
            }
            File.Delete(path);
            feedback.Message = $@"File deleted";
            feedback.Status = true;
            return Task.FromResult(feedback);
        }

        public IFeedback Exists(IObjectReadRequest input) {
            var feedback = new Feedback() { Status = false };
            var path = GetFinalStoragePath(input,forReadOnly:true); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) {
                feedback.Message = "Unable to generate path from provided inputs.";
                return feedback;
            }
            bool isFile = input.StorageRoutes.Any(p => p.IsFile);
            //If any of the storageroute has a IsFile flag, then it means, we are trying to figure out a file existence.
            if (isFile) {
                feedback.Status = File.Exists(path);
            } else {
                feedback.Status = Directory.Exists(path);
            }
            if (!feedback.Status) feedback.Message = $@"Does not exists {path}";
            return feedback;
        }

        public long GetSize(IObjectReadRequest input) {
            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return 0;
            return new FileInfo(path).Length;
        }

        public Task<IDirectoryInfoResponse> GetDirectoryInfo(IObjectReadRequest input) {
            IDirectoryInfoResponse result = new DirectoryInfoResponse() { Status = false};

            var path = GetFinalStoragePath(input,true); //This will also ensure we are not trying to delete something 
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
                var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
                if (string.IsNullOrWhiteSpace(path)) {
                    result.Message = $@"Unable to generate the path. Please check inputs.";
                    return Task.FromResult(result);
                }

                if (Directory.Exists(path)) {
                    result.Status = true;
                    result.Message = $@"Directory already exists.";
                    return Task.FromResult(result);
                }
                if (!EnsureDirectory(path)) {
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

        public Task<IFeedback> DeleteDirectory(IObjectReadRequest input, bool recursive) {
            IFeedback feedback = new Feedback() { Status = false };
            var path = GetFinalStoragePath(input,forReadOnly: true); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) {
                feedback.Message = "Unable to generate path from provided inputs.";
                return Task.FromResult(feedback);
            }
            //How do we verfiy, if this the final target that we wish to delete?
            //We should not by mistake end up deleting a wrong directory.
            var expectedToDelete = input.StorageRoutes.Last().Path?.ToLower().Trim();
            if (string.IsNullOrWhiteSpace(expectedToDelete) ||
                (expectedToDelete == "\\" || expectedToDelete == "/") ||
                expectedToDelete.Equals(BasePath.ToLower())) {
                feedback.Message = "Path is not valid for deleting.";
                return Task.FromResult(feedback);
            }
            if (!Directory.Exists(path)) {
                feedback.Message = $@"Directory does not exists. : {path}.";
                return Task.FromResult(feedback);
            }
            Directory.Delete(path, recursive);
            feedback.Status = true;
            feedback.Message = "Deleted successfully";
            return Task.FromResult(feedback);
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

        bool FilePreProcess(IObjectCreateResponse result, string filePath, ObjectExistsResolveMode conflict) {

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
            //if (input.StartsWith("/") || input.StartsWith(@"\")) input = input.Substring(1); //We cannot have something start with / as well
            if (input.Contains("..")) throw new ArgumentException("Path Contains invalid characters. Do not include double dots.");
            return input;
        }

        string Build(List<StorageRoute> routes, bool allow_root_access, bool forReadOnly) {
            string path = BasePath; //Base path cannot be null. it is mandatory for disk storage.
            //Pull the lastone out.
            if (routes == null && routes.Count < 1) return path; //Direclty create inside the basepath (applicable in few cases);
            //If one of the path is trying to make a root access, should we allow or deny?
            
            for (int i = 0; i < routes.Count; i++) { //the -2 is to ensure we ignore the last part.
                var route = routes[i];
                //If we are at the end, ignore
                string wv = route.Path;
                wv = SanitizePath(wv.Trim());
                if (string.IsNullOrWhiteSpace(wv)) {
                    if (allow_root_access) continue;
                    //We are trying a root access
                    throw new AccessViolationException("Root access is not allowed.");
                } 

                bool isEndPart = (i == routes.Count-1); //Are we at the end of the line?
                path = Path.Combine(path, wv);

                //If the route is a file, just jump out. Because, if it is a file, may be we are either uploading or fetching the file. the file might even contain it's own sub path as well. 
                if (route.IsFile) break;

                //1. a) Dir Creation disallowed b) Dir doesn't exists
                if (!route.CanCreatePath && !Directory.Exists(path)) {
                    //Whether it is a file or a directory, if user doesn't have access to create it, throw exception.
                    string errMsg = $@"Directory doesn't exists : {route.Key ?? route.Path}";

                    //2.1 ) Are we in the middle, trying to ensure some directory exists?
                    if (isEndPart && !forReadOnly) errMsg = $@"Access denied to create/delete the directory :{route.Key ?? route.Path}";
                    throw new ArgumentException(errMsg);
                }

                //3. Are we trying to create a directory as our main goal?
                if (isEndPart) break;

                if (!EnsureDirectory(path)) throw new ArgumentException($@"Unable to create the directory : {route.Key ?? route.Path}");
            }
            return path;
        }

        string GetFinalStoragePath(IObjectReadRequest input, bool allowRootAccess = false, bool forReadOnly= false) {
            if (input == null || !(input is ObjectReadRequest req)) throw new ArgumentNullException($@"{nameof(IObjectReadRequest)} cannot be null. It has to be of type {nameof(ObjectReadRequest)}");

            req.ObjectLocation = Build(input.StorageRoutes,allowRootAccess,forReadOnly); 
            //What if, the user provided no value and we end up with only the Basepath.
            if (string.IsNullOrWhiteSpace(req.ObjectLocation)) throw new ArgumentNullException($@"Unable to generate a full object path for the request");

            //If it doesn't start with base path, we replace as well
            if (!req.ObjectLocation.StartsWith(BasePath)) throw new ArgumentOutOfRangeException("The generated path is not accessible. Please check the inputs.");

            if (req.ObjectLocation.Contains("..")) throw new ArgumentOutOfRangeException("The generated path contains invalid characters. Please fix");

            return req.ObjectLocation;
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
