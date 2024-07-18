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

        public bool Delete(StorageRequestBase input) {
            if (!input.TryGeneratePath(out var path)) return false;
            string finalPath = Path.Combine(BasePath, path);
            if (File.Exists(finalPath)) {
                File.Delete(finalPath);
            }
            return true;
        }

        public bool Exists(StorageRequest input) {
            var fsIn= input.ToDiskStorage();
            string finalPath = Path.Combine(BasePath, fsIn.TargetPath);
            return File.Exists(finalPath);
        }

        public Stream Download(StorageRequestBase input) {
            if (!input.TryGeneratePath(out var path)) return null;
            string finalPath = Path.Combine(BasePath, path);
            if (!File.Exists(finalPath)) return null;
            return new FileStream(finalPath, FileMode.Open, FileAccess.Read); //Stream is open here.
        }

        public long GetSize(StorageRequestBase input) {
            if (!input.TryGeneratePath(out var path)) return 0;
            string finalPath = Path.Combine(BasePath, path);
            var finfo = new FileInfo(finalPath);
            return finfo.Length;
        }

        bool EnsureDirectory(string target) {
            try {
                //string target = Path.Combine(BasePath, path);
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

        public async Task<FileStorageSummary> Upload(StorageRequest input, Stream file,int bufferSize = 8192) {
            input.SanitizeTargetName(); // If a wrong target name is provided, we just reset it.
            //######### UPLOAD HAPPENS ONLY FOR FILES AND NOT FOR FOLDERS ##############.
            input.IsFolder = false; // we are uploading only files.

            if (bufferSize < 4096) bufferSize = 4096; //Default CopyTo from System.IO has 80KB buffersize. We setit as 4KB for fast storage.

            FileStorageSummary result = new FileStorageSummary() { Status = false , RawName = input.RawName};
            try {
                if (file == null) throw new ArgumentException($@"File stream is null. Nothing to save.");
                file.Position = 0; //Precaution
                var dReq = input.ToDiskStorage();
                string targetDir = Path.Combine(BasePath, Path.GetDirectoryName(dReq.TargetPath)); //we dont' try to get the directory name as it is the final

                if (!EnsureDirectory(targetDir)) {
                    result.Message = $@"Unable to ensure storage directory. Please check if it is valid.{targetDir}";
                    return result;
                }
                result.TargetName = dReq.TargetName; //this is the name which is used to store the file.. May be id or hash with or without extension.
                if (!string.IsNullOrWhiteSpace(dReq.RootDir)) {
                    result.BasePath = Path.Combine(BasePath, dReq.RootDir).ToLower(); //Need not show the split. Just keep it as base dir alone.
                } else {
                    result.BasePath = BasePath.ToLower();
                }
               
                string finalPath = Path.Combine(BasePath, dReq.TargetPath); //this includes the split file name.

                if (File.Exists(finalPath)) {
                    switch (input.ResolveMode) {
                        case StorageFileConflict.Skip:
                        result.Status = true;
                        return result; //Skip if it already exists.
                        case StorageFileConflict.ReturnError:
                        result.Status = false;
                        result.Message = $@"File already exists";
                        return result;
                        case StorageFileConflict.ThrowException:
                        throw new ArgumentException($@"File {dReq.TargetName} already exists in {result.BasePath}");
                    }
                }

                using (var fs = File.Create(finalPath)) {
                    await file.CopyToAsync(fs,bufferSize);
                }

                result.Status = true;
                result.Size = file.Length; //storage size in bytes.
            } catch (Exception ex) {
                result.Status = false;
                result.Message = ex.Message;
            }
            return result;
        }
    }
}
