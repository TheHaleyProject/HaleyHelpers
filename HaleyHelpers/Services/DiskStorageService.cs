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

        string GetFinalStoragePath(ObjectReadRequest input) {
            if (input == null) throw new ArgumentNullException($@"{nameof(ObjectReadRequest)} cannot be null");

            //What if, the user provided no value and we end up with only the Basepath.
            if (string.IsNullOrWhiteSpace(input.Name) && string.IsNullOrWhiteSpace(input.FullPath)) throw new ArgumentNullException($@"Either Name of Path is required to generate a full path");


            string path = input.FullPath?.ToLower();

            //If the path is null, we set the base path.
            if (string.IsNullOrWhiteSpace(path)) path = BasePath;

            //If it doesn't start with base path, we replace as well
            if (!path.StartsWith(BasePath)) path = Path.Combine(BasePath, path);

            //Check if the name and path ends with same values.
            if (!string.IsNullOrWhiteSpace(input.Name) && !path.EndsWith(input.Name.ToLower())) path = Path.Combine(path, input.Name.ToLower());

            //PRECAUTION : Final check to ensure that we didn't make any mistake.
            if (!path.StartsWith(BasePath)) {
                throw new ArgumentOutOfRangeException("The generated path is not accessible. Please check the inputs.");
            }

            return path;

        }

        #region Disk Storage Management 
        Task<SummaryResponse> Upload(ObjectWriteRequest input, Stream file, int bufferSize) {
            throw new NotImplementedException();
        }

        public Task<StreamResponse> Download(ObjectReadRequest input, bool auto_search_extension = true) {
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

        public Task<bool> Delete(ObjectReadRequest input) {
            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(false);
           
            if (File.Exists(path)) {
                File.Delete(path);
            }
            return Task.FromResult(true);
        }

        public bool Exists(ObjectReadRequest input) {
            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) return false;
            return File.Exists(path);
        }

        public long GetSize(ObjectReadRequest input) {
            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return 0;
            return new FileInfo(path).Length;
        }

        public Task<DirectoryInfoResponse> GetInfo(ObjectReadRequest input) {
            RepoSummary result = new RepoSummary() { Path = input.Path };
            if (input == null || input.RepoInfo == null) throw new ArgumentException("Not a valid request for downloading from repo.");
            if (!input.RepoInfo.TryGeneratePath(true, out var repo_path)) return Task.FromResult(result);
            string finalPath = Path.Combine(BasePath, repo_path);

            if (!string.IsNullOrWhiteSpace(input.Path)) {
                finalPath = Path.Combine(finalPath, input.Path);
            }

            //Now, get all information from this location and return back.
            if (!Directory.Exists(finalPath)) {
                result.Message = "Directory doesn't exists";
                return Task.FromResult(result);
            }

            if (!finalPath.StartsWith(BasePath)) {
                result.Message = "Not Autuhorized to check this folder. Please check the path.";
                return Task.FromResult(result);
            }

            var dinfo = new DirectoryInfo(finalPath);

            result.FoldersList = dinfo.GetDirectories()?.Select(p => p.Name)?.ToList();
            result.FilesList = dinfo.GetFiles()?.Select(p => p.Name)?.ToList();
            return Task.FromResult(result);
        }

        public Task<StorageSummary> CreateRepository(ObjectWriteRequest input) {
            input.SanitizeTargetName(); // If a wrong target name is provided, we just reset it.
            input.Source = StorageNameSource.Id; //Because we will not have name.

            StorageSummary result = new StorageSummary() { Status = false, RawName = input.Id }; //remember, we allow only ID to be present from the VaultFolder create request.
            try {
                var dReq = input.ToDiskStorage(true);
                string targetDir = Path.Combine(BasePath, dReq.TargetPath); //target path will not contain extension, if it is a folder.
                result.SavedName = dReq.ObjectFinalName;

                if (!targetDir.StartsWith(BasePath)) {
                    throw new ArgumentOutOfRangeException("Not authorized for this folder. Please check the path.");
                }

                if (Directory.Exists(targetDir)) {
                    result.Message = $@"Directory already exists.";
                    return Task.FromResult(result);
                }
                if (!EnsureDirectory(targetDir)) {
                    result.Message = $@"Unable to ensure storage directory. Please check if it is valid.{targetDir}";
                    return Task.FromResult(result);
                }

                result.Status = true;
            } catch (Exception ex) {
                result.Status = false;
                result.Message = ex.Message;
            }
            return Task.FromResult(result);
        }

        public Task<bool> DeleteRepository(ObjectReadRequest input, bool recursive) {
            var path = GetFinalStoragePath(input); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return Task.FromResult(false);
            return new FileInfo(path).Length;
            if (!input.TryGeneratePath(true, out var path)) return Task.FromResult(false);
            string finalPath = Path.Combine(BasePath, path);

            if (!finalPath.StartsWith(BasePath)) {
                throw new ArgumentOutOfRangeException("Not authorized for this folder. Please check the path.");
            }

            if (Directory.Exists(finalPath)) {
                Directory.Delete(finalPath, recursive);
            }
            return Task.FromResult(true);
        }

        #endregion


        #region Vault

        public async Task<FileStorageSummary> Upload(ObjectWriteRequest input, Stream file, int bufferSize = 8192) {
            input.SanitizeTargetName(); // If a wrong target name is provided, we just reset it.
            //######### UPLOAD HAPPENS ONLY FOR FILES AND NOT FOR FOLDERS ##############.

            if (bufferSize < 4096) bufferSize = 4096; //Default CopyTo from System.IO has 80KB buffersize. We setit as 4KB for fast storage.

            FileStorageSummary result = new FileStorageSummary() { Status = false, ObjectRawName = input.RawName };
            try {
                if (file == null) throw new ArgumentException($@"File stream is null. Nothing to save.");
                file.Position = 0; //Precaution
                var dReq = input.ToDiskStorage(false);
                string finalPath = Path.Combine(BasePath, dReq.TargetPath); //this includes the split file name.

                result.ObjectSavedName = dReq.ObjectFinalName; //this is the name which is used to store the file.. May be id or hash with or without extension.

                if (!FilePreProcess(result, input.ContainerName, finalPath, input.ResolveMode)) return result;

                using (var fs = File.Create(finalPath)) {
                    await file.CopyToAsync(fs, bufferSize);
                }

                if (!result.ObjectExists) result.Message = "Uploaded.";
                result.Status = true;
                result.Size = file.Length; //storage size in bytes.
            } catch (Exception ex) {
                result.Status = false;
                result.Message = ex.Message;
            }
            return result;
        }

       

        public Task<bool> DeleteRepository(StorageRequestBase input, bool recursive) {
            if (!input.TryGeneratePath(true, out var path)) return Task.FromResult(false);
            string finalPath = Path.Combine(BasePath, path);

            if (!finalPath.StartsWith(BasePath)) {
                throw new ArgumentOutOfRangeException("Not authorized for this folder. Please check the path.");
            }

            if (Directory.Exists(finalPath)) {
                Directory.Delete(finalPath, recursive);
            }
            return Task.FromResult(true);
        }
        #endregion

        bool FilePreProcess(FileStorageSummary result, string rootDir, string filePath, ObjectExistsResolveMode conflict) {

            var targetDir = Path.GetDirectoryName(filePath);
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
                    //case StorageFileConflict.ThrowException:
                    //throw new ArgumentException($@"File {Path.GetFileName(filePath)} already exists in {rootDir}");
                }
            }
            return true;
        }

       

        #region Repository
        public async Task<FileStorageSummary> UploadToRepo(RepoStorageRequest input, Stream file, int bufferSize = 8192) {
            FileStorageSummary result = new FileStorageSummary() { Status = false };
            try {
                if (file == null) throw new ArgumentException($@"File stream is null. Nothing to save.");
                file.Position = 0; //Precaution

                if (input == null || input.RepoInfo == null || !input.RepoInfo.TryGeneratePath(true, out string repo_path) || string.IsNullOrWhiteSpace(repo_path)) return result;

                if (bufferSize < 4096) bufferSize = 4096; //Default CopyTo from System.IO has 80KB buffersize. We setit as 4KB for fast storage.
                string targetRepo = Path.Combine(BasePath, repo_path); 
                result.ObjectSavedName = input.Name; //Will be the final 

                //Validate Target Repo
                if (!Directory.Exists(targetRepo)) {
                    result.Message = $@"Target Repository for the provided input doesn't exists";
                    return result;
                }

                string finalPath = targetRepo;

                //Now attach the repo path, along with the files path
                if (!string.IsNullOrWhiteSpace(input.Path)) {
                    finalPath = Path.Combine(finalPath, input.Path);  // Include the path.
                }

                //To the final file path, add the file name as well.
                finalPath = Path.Combine(finalPath, input.Name);

                if(!FilePreProcess(result,input.Path,finalPath,input.ResolveMode)) return result; 

                using (var fs = File.Create(finalPath)) {
                    await file.CopyToAsync(fs, bufferSize);
                }
                if (!result.ObjectExists) result.Message = "Uploaded.";
                result.Status = true;
                result.Size = file.Length; //storage size in bytes.
            } catch (Exception ex) {
                result.Status = false;
                result.Message = ex.Message;
            }
            return result;

        }

        public Task<RepoSummary> ReadRepoInfo(RepoStorageRequestBase input) {
            RepoSummary result = new RepoSummary() { Path = input.Path };
            if (input == null || input.RepoInfo == null) throw new ArgumentException("Not a valid request for downloading from repo.");
            if (!input.RepoInfo.TryGeneratePath(true, out var repo_path)) return Task.FromResult(result);
            string finalPath = Path.Combine(BasePath, repo_path);

            if (!string.IsNullOrWhiteSpace(input.Path)) {
                finalPath = Path.Combine(finalPath, input.Path);
            }

            //Now, get all information from this location and return back.
            if (!Directory.Exists(finalPath)) {
                result.Message = "Directory doesn't exists";
                return Task.FromResult(result);
            }

            if (!finalPath.StartsWith(BasePath)) {
                result.Message = "Not Autuhorized to check this folder. Please check the path.";
                return Task.FromResult(result);
            }

            var dinfo = new DirectoryInfo(finalPath);

            result.FoldersList = dinfo.GetDirectories()?.Select(p => p.Name)?.ToList();
            result.FilesList = dinfo.GetFiles()?.Select(p => p.Name)?.ToList();
            return Task.FromResult(result);
        }

        string GetRepoFinalPath(RepoStorageRequestBase input) {
            if (input == null || input.RepoInfo == null) throw new ArgumentException("Not a valid request for downloading from repo.");
            if (!input.RepoInfo.TryGeneratePath(true, out var repo_path)) return null;
            //If repository doesn't exists.. throw exception.
            if (!Directory.Exists(Path.Combine(BasePath, repo_path))) {
                throw new ArgumentException($@"Repository doesn't exists for {input.RepoInfo.ObjectSavedName}.");
            }

            string finalPath = Path.Combine(BasePath, repo_path);

            if (!string.IsNullOrWhiteSpace(input.Path)) {
                finalPath = Path.Combine(finalPath, input.Path);
            }

            if (!string.IsNullOrWhiteSpace(input.Name)) {
                finalPath = Path.Combine(finalPath, input.Name);
            }

            //Ensure that the path the user tries to open is from basepath.
            if (!finalPath.StartsWith(BasePath)) {
                throw new ArgumentOutOfRangeException("Not authorized for this folder. Please check the path.");
            }

            return finalPath;
        }

        public Task<Stream> DownloadFromRepo(RepoStorageRequestBase input) {
            try {
                var finalPath = GetRepoFinalPath(input);
                if (string.IsNullOrWhiteSpace(finalPath) || !File.Exists(finalPath)) return Task.FromResult(Stream.Null);
                return Task.FromResult(new FileStream(finalPath, FileMode.Open, FileAccess.Read) as Stream); //Stream is open here.
            } catch (Exception) {
                return Task.FromResult(Stream.Null);
            }
        }
        public Task<StorageResponse> DeleteFromRepo(RepoStorageRequestBase input) {
            StorageResponse result = new StorageResponse() { Status = false };
            try {
                var finalPath = GetRepoFinalPath(input);
                if (string.IsNullOrWhiteSpace(finalPath) || !File.Exists(finalPath)) {
                    result.Message = "File doesn't exists";
                    return Task.FromResult(result);
                }
                File.Delete(finalPath);
                result.Status = true;
                return Task.FromResult(result);
            } catch (Exception ex) {
                result.Message = ex.Message;
                result.Status = false;
                return Task.FromResult(result);
            }
            
        }
        public Task<StorageResponse> CreateFolderInRepo(RepoStorageRequestBase input) {
            StorageResponse result = new StorageResponse() { Status = false};
            try {
                var finalPath = GetRepoFinalPath(input);
                if (string.IsNullOrWhiteSpace(finalPath)) {
                    result.Message = $@"Unable to generate a valid path with given inputs. Repo Path : {input.RepoInfo.ObjectSavedName}. Folder path : {input.Path}";
                    return Task.FromResult(result);
                }
                //If final path exists.
                if (Directory.Exists(finalPath)) {
                    result.Status = true;
                    result.Message = "Directory already exists.";
                    return Task.FromResult(result);
                }
                if (!EnsureDirectory(finalPath)) {
                    result.Status = false;
                    result.Message = "Unable to create the directory";
                    return Task.FromResult(result);
                }
                result.Status = true;
                return Task.FromResult(result);
            } catch (Exception ex) {
                result.Status = false;
                result.Message = ex.Message;
                return Task.FromResult(result);
            }
        }
        public Task<StorageResponse> DeleteFolderInRepo(RepoStorageRequestBase input, bool recursive) {
            StorageResponse result = new StorageResponse() { Status = false };
            try {
                var finalPath = GetRepoFinalPath(input);
                if (string.IsNullOrWhiteSpace(finalPath)) {
                    result.Message = $@"Unable to generate a valid path with given inputs. Repo Path : {input.RepoInfo.ObjectSavedName}. Folder path : {input.Path}";
                    return Task.FromResult(result);
                }

                if (Directory.Exists(finalPath)) {
                    Directory.Delete(finalPath, recursive);
                } else {
                    result.Message = "Directory doesn't exists. Nothing to delete";
                }
                result.Status = true;
                return Task.FromResult(result);
            } catch (Exception ex) {
                result.Status = false;
                result.Message = ex.Message;
                return Task.FromResult(result);
            }
        }

     
        #endregion
    }
}
