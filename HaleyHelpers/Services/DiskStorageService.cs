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
    public class DiskStorageService : IStorageService {

        public DiskStorageService(string basePath) {
            BasePath = basePath;
            //This is supposedly the directory where all storage goes into.
            if (BasePath == null) {
                BasePath = AssemblyUtils.GetBaseDirectory(parentFolder: "DataStore");
            }
        }

        public string BasePath { get; private set; }

        #region Vault
        public Task<bool> Delete(StorageRequestBase input) {
            if (!input.TryGeneratePath(false,out var path)) return Task.FromResult(false);
            string finalPath = Path.Combine(BasePath, path);
            if (!finalPath.StartsWith(BasePath)) {
                throw new ArgumentOutOfRangeException("Not authorized for this folder. Please check the path.");
            }
            if (File.Exists(finalPath)) {
                File.Delete(finalPath);
            }
            return Task.FromResult(true);
        }

        public bool Exists(StorageRequest input) {
            var fsIn = input.ToDiskStorage(false); //we check only for file during exists. 
            string finalPath = Path.Combine(BasePath, fsIn.TargetPath);
            return File.Exists(finalPath);
        }

        public Task<StorageStreamResponse> Download(StorageRequestBase input) {
            StorageStreamResponse result = new StorageStreamResponse() { Status = false, Stream = Stream.Null };
            if (!input.TryGeneratePath(false, out var path) || string.IsNullOrWhiteSpace(path)) {
                result.Message = "Unable to generate file path.";
                return Task.FromResult(result);
            }
            string finalPath = Path.Combine(BasePath, path);

            if (!finalPath.StartsWith(BasePath)) {
                result.Message = "Not authorized for this folder. Please check the path.";
                return Task.FromResult(result);
            }

            if (!File.Exists(finalPath)) {

                if (string.IsNullOrWhiteSpace(Path.GetExtension(input.TargetName))) {
                    var findName = Path.GetFileNameWithoutExtension(finalPath);
                    //Extension not provided. So, lets to see if we have any matching file.
                    DirectoryInfo dinfo = new DirectoryInfo(Path.GetDirectoryName(finalPath));
                    var matchingFiles = dinfo?.GetFiles()?.Where(p => Path.GetFileNameWithoutExtension(p.Name) == findName).ToList();
                    if (matchingFiles.Count() == 1) {
                        finalPath = matchingFiles.FirstOrDefault().FullName;
                    }
                }
            }

            if (!File.Exists(finalPath)) {
                result.Message = "File doesn't exist.";
                return Task.FromResult(result);
            }
            result.Status = true;
            result.Extension = Path.GetExtension(finalPath); 
            result.Stream = new FileStream(finalPath, FileMode.Open, FileAccess.Read) as Stream;
            return Task.FromResult(result); //Stream is open here.
        }

        public long GetSize(StorageRequestBase input) {
            if (!input.TryGeneratePath(false,out var path)) return 0;
            string finalPath = Path.Combine(BasePath, path);
            var finfo = new FileInfo(finalPath);
            return finfo.Length;
        }

        public async Task<FileStorageSummary> Upload(StorageRequest input, Stream file, int bufferSize = 8192) {
            input.SanitizeTargetName(); // If a wrong target name is provided, we just reset it.
            //######### UPLOAD HAPPENS ONLY FOR FILES AND NOT FOR FOLDERS ##############.

            if (bufferSize < 4096) bufferSize = 4096; //Default CopyTo from System.IO has 80KB buffersize. We setit as 4KB for fast storage.

            FileStorageSummary result = new FileStorageSummary() { Status = false, RawName = input.RawName };
            try {
                if (file == null) throw new ArgumentException($@"File stream is null. Nothing to save.");
                file.Position = 0; //Precaution
                var dReq = input.ToDiskStorage(false);
                string finalPath = Path.Combine(BasePath, dReq.TargetPath); //this includes the split file name.

                result.TargetName = dReq.TargetName; //this is the name which is used to store the file.. May be id or hash with or without extension.

                if (!FilePreProcess(result, input.Container, finalPath, input.ResolveMode)) return result;

                using (var fs = File.Create(finalPath)) {
                    await file.CopyToAsync(fs, bufferSize);
                }

                if (!result.FileExists) result.Message = "Uploaded.";
                result.Status = true;
                result.Size = file.Length; //storage size in bytes.
            } catch (Exception ex) {
                result.Status = false;
                result.Message = ex.Message;
            }
            return result;
        }

        public Task<StorageSummary> CreateRepository(StorageRequest input) {
            input.SanitizeTargetName(); // If a wrong target name is provided, we just reset it.
            input.Source = StorageNameSource.Id; //Because we will not have name.

            StorageSummary result = new StorageSummary() { Status = false, RawName = input.Id }; //remember, we allow only ID to be present from the VaultFolder create request.
            try {
                var dReq = input.ToDiskStorage(true);
                string targetDir = Path.Combine(BasePath, dReq.TargetPath); //target path will not contain extension, if it is a folder.
                result.TargetName = dReq.TargetName;

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

        bool FilePreProcess(FileStorageSummary result, string rootDir, string filePath, StorageFileConflict conflict) {

            var targetDir = Path.GetDirectoryName(filePath);
            if (!EnsureDirectory(targetDir)) {
                result.Message = $@"Unable to ensure storage directory. Please check if it is valid. {targetDir}";
                return false;
            }

            if (!filePath.StartsWith(BasePath)) {
                result.Message = "Not authorized for this folder. Please check the path.";
                return false;
            }

            result.FileExists = File.Exists(filePath);
            if (result.FileExists) {
                switch (conflict) {
                    case StorageFileConflict.Skip:
                    result.Status = true;
                    result.Message = "File exists. Skipped";
                    return true; //Skip if it already exists.
                    case StorageFileConflict.ReturnError:
                    result.Status = false;
                    result.Message = $@"File Exists. Returned Error.";
                    return false;
                    case StorageFileConflict.Replace:
                    result.Message = "Replace initiated";
                    return true;
                    //case StorageFileConflict.ThrowException:
                    //throw new ArgumentException($@"File {Path.GetFileName(filePath)} already exists in {rootDir}");
                }
            }
            return true;
        }

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

        #region Repository
        public async Task<FileStorageSummary> UploadToRepo(RepoStorageRequest input, Stream file, int bufferSize = 8192) {
            FileStorageSummary result = new FileStorageSummary() { Status = false };
            try {
                if (file == null) throw new ArgumentException($@"File stream is null. Nothing to save.");
                file.Position = 0; //Precaution

                if (input == null || input.RepoInfo == null || !input.RepoInfo.TryGeneratePath(true, out string repo_path) || string.IsNullOrWhiteSpace(repo_path)) return result;

                if (bufferSize < 4096) bufferSize = 4096; //Default CopyTo from System.IO has 80KB buffersize. We setit as 4KB for fast storage.
                string targetRepo = Path.Combine(BasePath, repo_path); 
                result.TargetName = input.Name; //Will be the final 

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
                if (!result.FileExists) result.Message = "Uploaded.";
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
                throw new ArgumentException($@"Repository doesn't exists for {input.RepoInfo.TargetName}.");
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
        public Task<StorageResponseBase> DeleteFromRepo(RepoStorageRequestBase input) {
            StorageResponseBase result = new StorageResponseBase() { Status = false };
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
        public Task<StorageResponseBase> CreateFolderInRepo(RepoStorageRequestBase input) {
            StorageResponseBase result = new StorageResponseBase() { Status = false};
            try {
                var finalPath = GetRepoFinalPath(input);
                if (string.IsNullOrWhiteSpace(finalPath)) {
                    result.Message = $@"Unable to generate a valid path with given inputs. Repo Path : {input.RepoInfo.TargetName}. Folder path : {input.Path}";
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
        public Task<StorageResponseBase> DeleteFolderInRepo(RepoStorageRequestBase input, bool recursive) {
            StorageResponseBase result = new StorageResponseBase() { Status = false };
            try {
                var finalPath = GetRepoFinalPath(input);
                if (string.IsNullOrWhiteSpace(finalPath)) {
                    result.Message = $@"Unable to generate a valid path with given inputs. Repo Path : {input.RepoInfo.TargetName}. Folder path : {input.Path}";
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
