﻿using Haley.Abstractions;
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

        public static bool EnsureDirectory(this string target) {
            try {
                if (string.IsNullOrWhiteSpace(target)) return false;
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

            req.ObjectLocation = input.StorageRoutes?.BuildStoragePath(basePath, allowRootAccess, readonlyMode);
            //What if, the user provided no value and we end up with only the Basepath.
            if (string.IsNullOrWhiteSpace(req.ObjectLocation)) throw new ArgumentNullException($@"Unable to generate a full object path for the request");

            //If it doesn't start with base path, we replace as well
            if (!req.ObjectLocation.StartsWith(basePath)) throw new ArgumentOutOfRangeException("The generated path is not accessible. Please check the inputs.");

            if (req.ObjectLocation.Contains("..")) throw new ArgumentOutOfRangeException("The generated path contains invalid segments. Parent directory access is not allowed. Please fix");

            return req.ObjectLocation;
        }

        public static string BuildStoragePath(this List<StorageRoute> routes, string basePath, bool allow_root_access, bool readonlyMode) {
            string path = basePath; //Base path cannot be null. it is mandatory for disk storage.
            //Pull the lastone out.
            if (routes == null && routes.Count < 1) return path; //Direclty create inside the basepath (applicable in few cases);
                                                                 //If one of the path is trying to make a root access, should we allow or deny?

            for (int i = 0; i < routes.Count; i++) { //the -2 is to ensure we ignore the last part.
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

                //1. a) Dir Creation disallowed b) Dir doesn't exists
                if (!(route.CreateIfMissing && Directory.Exists(path))) {
                    //Whether it is a file or a directory, if user doesn't have access to create it, throw exception.
                    string errMsg = $@"Directory doesn't exists : {route.Key ?? route.Path}";

                    //2.1 ) Are we in the middle, trying to ensure some directory exists?
                    if (isEndPart && !readonlyMode) errMsg = $@"Access denied to create/delete the directory :{route.Key ?? route.Path}";
                    throw new ArgumentException(errMsg);
                }

                //3. Are we trying to create a directory as our main goal?
                if (isEndPart) break;

                if (!(path?.EnsureDirectory() ?? false)) throw new ArgumentException($@"Unable to create the directory : {route.Key ?? route.Path}");
            }
            return path;
        }
    }
}
