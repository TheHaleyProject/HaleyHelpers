using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Haley.Utils
{
    public static class StorageUtils {
        public static string SanitizePath(this string input) {
            if (string.IsNullOrWhiteSpace(input)) return input;
            input = input.Trim();
            if (input == "/" || input == @"\") input = string.Empty; //We cannot have single '/' as path.
            //if (input.StartsWith("/") || input.StartsWith(@"\")) input = input.Substring(1); //We cannot have something start with / as well
            //Reason is the user can then directly add .. and go to previous folders and access root path which is not allowed.
            if (input.Contains("..")) throw new ArgumentException("Path Contains invalid segment. Access to parent directory is not allowed.");
            return input;
        }

        public static (string name, string path, Guid guid) GetBasePath(string name, bool iscontrolled,int split_length = 2, int depth = 0) {
            if (string.IsNullOrWhiteSpace(name)) return (string.Empty, string.Empty, Guid.Empty);
            var dbname = name.ToDBName();
            var hashguid = dbname.CreateGUID(HashMethod.Sha256);
            string path = dbname;

            if (depth < 0) depth = 0;
            if (depth > 8) depth = 8;

            if (split_length < 1) split_length = 1;
            if (split_length > 8) split_length = 8;

            if (iscontrolled) {
                if (long.TryParse(name, out long res)) {
                    depth = 0; //For number lets reset depth as 0.
                    path = res.ToString().Separate(split_length,depth, resultAsPath: true);
                    //When we deal with numbers, we end the folder with 'd' to denote it as directory and to conflict with other directories.
                    path += "d"; //ending with 'd'
                } else {
                    if (depth < 1) depth = 4; //We cannot have unlimited depth split for GUID.
                    path = hashguid.ToString().Replace("-", "").Separate(split_length,depth, addPadding: false, resultAsPath: true);
                }
            }
            return (dbname, path, hashguid);
        }

        public static string PathFromGUID(this Guid guid, int split_length = 2, int depth = 0) {
            return PathFromGUID(guid.ToString(),split_length,depth);
        }

        public static string PathFromGUID(this string guid, int split_length = 2, int depth = 0) {
            if (depth < 1) depth = 4; //For guid , we cannot have depth at 0
            if (depth > 8) depth = 8;

            if (split_length < 1) split_length = 1;
            if (split_length > 8) split_length = 8;
            return guid.Replace("-", "").Separate(split_length, depth, addPadding: false, resultAsPath: true);
        }

        public static async Task<bool> TryReplaceFileAsync(this Stream sourceStream, string path, int bufferSize, int maxRetries = 5, int delayMilliseconds = 500) {
            for (int attempt = 0; attempt < maxRetries; attempt++) {
                try {
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        await sourceStream.CopyToAsync(fs, bufferSize);
                        return true;
                    }
                } catch (IOException ex) {
                    await Task.Delay(delayMilliseconds);
                }
            }

            return false;
        }

        public static async Task<bool> TryDeleteDirectory(this string path, int maxRetries = 5, int delayMs = 500) {
            if (!Directory.Exists(path)) return false;

            //Directory.Delete(path, true); //If a file is locked, the process is stopped. So better do them individually.

            //TODO: Should we go for parallel delete? with concurrency to speed up things?
            foreach (var file in Directory.GetFiles(path)) {
                await TryDeleteFile(file, maxRetries, delayMs);
            }

            foreach (var dir in Directory.GetDirectories(path)) {
                await TryDeleteDirectory(dir, maxRetries, delayMs);
            }

            for (int attempt = 0; attempt < maxRetries; attempt++) {
                try {
                    Directory.Delete(path, false); // At this point it's empty
                    return true;
                } catch (IOException) {
                    await Task.Delay(delayMs);
                }
            }

            return false;
        }

        public static async Task<bool> TryCreateDirectory(this string path, int maxRetries = 5, int delayMs = 500) {
            if (Directory.Exists(path)) return true;

            for (int attempt = 0; attempt < maxRetries; attempt++) {
                try {
                    Directory.CreateDirectory(path); 
                    return true;
                } catch (IOException) {
                    await Task.Delay(delayMs);
                }
            }

            return false;
        }

        public static async Task<bool> TryDeleteFile(this string path, int maxRetries = 5, int delayMilliseconds = 500) {
            for (int attempt = 0; attempt < maxRetries; attempt++) {
                try {
                    if (File.Exists(path)) {
                        File.SetAttributes(path, FileAttributes.Normal); // In case it's read-only
                        File.Delete(path);
                    }
                    return true;
                } catch (IOException ex) {
                    await Task.Delay(delayMilliseconds);
                }
            }
            return false;
        }

        public static string BuildStoragePath(this IObjectReadRequest input, string basePath, bool allowRootAccess = false, bool readonlyMode = false) {
            if (input == null || !(input is ObjectReadRequest req)) throw new ArgumentNullException($@"{nameof(IObjectReadRequest)} cannot be null. It has to be of type {nameof(ObjectReadRequest)}");

            if (basePath.Contains("..")) throw new ArgumentOutOfRangeException("The base path contains invalid segments. Parent directory access is not allowed. Please fix");
            if (!Directory.Exists(basePath)) throw new DirectoryNotFoundException("Base directory not found. Please ensure it is present");
            if (string.IsNullOrWhiteSpace(req.TargetPath)) {
                req.TargetPath = input.StorageRoutes?.BuildStoragePath(basePath, allowRootAccess, readonlyMode);
            } else {
                req.TargetPath = Path.Combine(basePath, req.TargetPath);
            }

            //What if, the user provided no value and we end up with only the Basepath.
            if (string.IsNullOrWhiteSpace(req.TargetPath)) throw new ArgumentNullException($@"Unable to generate a full object path for the request");

            if (req.TargetPath.Contains("..")) throw new ArgumentOutOfRangeException("The generated path contains invalid segments. Parent directory access is not allowed. Please fix");

            return req.TargetPath;
        }

        static string JoinBasePaths(List<string> paths) {
            if (paths == null || paths.Count < 1) {
                throw new ArgumentNullException("Base paths not found. Please provide a valid base path to proceed.");
            }
           return Path.Combine(paths.ToArray()); //Will it be in proper order??
        }

        public static string BuildStoragePath(this List<StorageRoute> routes, string basePath, bool allow_root_access, bool readonlyMode) {

            string path = basePath;  
            if (!Directory.Exists(path)) throw new ArgumentException("BasePath Directory doesn't exists.");

            //Pull the lastone out.
            if (routes == null && routes.Count < 1) return path; //Direclty create inside the basepath (applicable in few cases);
                                                                 //If one of the path is trying to make a root access, should we allow or deny?

            for (int i = 0; i < routes.Count; i++) { 
                var route = routes[i];
                //If we are at the end, ignore
                string value = route.Path;
                value = SanitizePath(value);

                if (string.IsNullOrWhiteSpace(value)) {
                    if (allow_root_access) continue;
                    //We are trying a root access
                    throw new AccessViolationException("Root directory access is not allowed.");
                }

                bool isEndPart = (i == routes.Count - 1); //Are we at the end of the line?
                path = Path.Combine(path, value);

                //If the route is a file, just jump out. Because, if it is a file, may be we are either uploading or fetching the file. the file might even contain it's own sub path as well. 
                if (route.IsFile) break;

                //1. a) Dir Creation disallowed b) Dir doesn't exists c) The route is not of type Client or Module. 
                if (!Directory.Exists(path) && !route.CreateIfMissing) {
                    //Whether it is a file or a directory, if user doesn't have access to create it, throw exception.
                    //We cannot allow to create Client & Module paths.
                    string errMsg = $@"Directory doesn't exists : {route.Key ?? route.Path}";

                    //2.1 ) Are we in the middle, trying to ensure some directory exists?
                    if (isEndPart && !readonlyMode) errMsg = $@"Access denied to create/delete the directory :{route.Key ?? route.Path}";
                    throw new ArgumentException(errMsg);
                }

                //3. Are we trying to create a directory as our main goal?
                if (isEndPart) break;
                if (!(path?.TryCreateDirectory().Result ?? false)) throw new ArgumentException($@"Unable to create the directory : {route.Key ?? route.Path}");
            }

            if (path.StartsWith(basePath)) throw new ArgumentOutOfRangeException("The generated path is not accessible. Please check the inputs.");
            return path;
        }
    }
}
