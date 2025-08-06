using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Haley.Utils
{
    public static class OSSUtils {
        static (int length,int depth) defaultSplitProvider(bool isInputNumber) {
            if (!isInputNumber) return (2, 6); //Split by 2 and go upto 6 depth.
            return (2, 0); //For number go full round
        }
        public static string SanitizePath(this string input) {
            if (string.IsNullOrWhiteSpace(input)) return input;
            input = input.Trim();
            if (input == "/" || input == @"\") input = string.Empty; //We cannot have single '/' as path.
            //if (input.StartsWith("/") || input.StartsWith(@"\")) input = input.Substring(1); //We cannot have something start with / as well
            //Reason is the user can then directly add .. and go to previous folders and access root path which is not allowed.
            if (input.Contains("..")) throw new ArgumentException("Path Contains invalid segment. Access to parent directory is not allowed.");
            return input;
        }

        public static (string name, string path) GenerateFileSystemSavePath(IOSSControlled nObj,OSSParseMode? parse_overwrite = null, Func<bool,(int length,int depth)> splitProvider = null, string suffix = null, Func<string,long> idGenerator = null,bool throwExceptions = false) {
            if (nObj == null || !nObj.TryValidate(out _)) return (string.Empty, string.Empty);
            string result = string.Empty;
            long objId = 0;
            Guid objGuid = Guid.Empty;
            switch (nObj.ControlMode) {
                case OSSControlMode.None:
                    nObj.SaveAsName = nObj.Name;
                break;
                case OSSControlMode.Number:
                if (nObj.Name.TryPopulateControlledID(out objId, parse_overwrite ?? nObj.ParseMode, idGenerator, throwExceptions)) {
                    nObj.SaveAsName = objId.ToString();
                }
                break;
                case OSSControlMode.Guid:
                 if (nObj.Name.TryPopulateControlledGUID(out objGuid, parse_overwrite ?? nObj.ParseMode, throwExceptions)) {
                    nObj.SaveAsName = objGuid.ToString("N");
                }
                break;
                case OSSControlMode.Both:
                //In case of both, the problem is, we need to first ensure, we are able to parse them.. and then only go ahead with generating.
                //Focus on parsing first and then if doesn't work, hten jump to generate. Even in that case we need to see through thenend.
                if (nObj.Name.TryPopulateControlledID(out objId, OSSParseMode.Parse, idGenerator, false)) {
                    nObj.SaveAsName = objId.ToString();
                } else if(nObj.Name.TryPopulateControlledGUID(out objGuid, OSSParseMode.Parse, false)){
                    nObj.SaveAsName = objGuid.ToString("N");
                } else if (nObj.Name.TryPopulateControlledID(out objId, parse_overwrite ?? nObj.ParseMode, idGenerator, false)) {
                    //Try with original parsing mode, ,may be we are asked to generte. We dont' know;
                    nObj.SaveAsName = objId.ToString();
                } else if (nObj.Name.TryPopulateControlledGUID(out objGuid, parse_overwrite ?? nObj.ParseMode, throwExceptions)) {
                    nObj.SaveAsName = objGuid.ToString("N");
                }
                break;
            }
            
            result = PreparePath(nObj.SaveAsName, splitProvider, nObj.ControlMode);
            
            //If extension is missing, check for extension. (only for controlled paths, extension would be missing)
            if (nObj.ControlMode != OSSControlMode.None) {
                
                if (!string.IsNullOrWhiteSpace(suffix)) {
                    //If we are dealing with number and also inside some kind of control mode, add suffix.
                    result += $@"{suffix}"; //Never get _ as suffix.
                }

                //Populate methods would have removed the Extensions. We add them back.
                var extension = Path.GetExtension(nObj.Name);

                //Add extension if exists.
                if (!string.IsNullOrWhiteSpace(extension)) {
                    result += $@"{extension}";
                }
            }

            //We add suffix for all controlled paths.
            return (nObj.SaveAsName, result);
        }

        public static string PreparePath(string input, Func<bool, (int length, int depth)> splitProvider = null, OSSControlMode control_mode = OSSControlMode.None) {
            if (string.IsNullOrWhiteSpace(input) || control_mode == OSSControlMode.None) return input;
            if (splitProvider == null) splitProvider = defaultSplitProvider;
            bool isNumber = input.IsNumber();
            var sinfo = splitProvider(isNumber);
            if (sinfo.depth < 0) sinfo.depth = 0;
            if (sinfo.depth > 12) sinfo.depth = 12;

            if (sinfo.length < 1) sinfo.length = 1;
            if (sinfo.length > 8) sinfo.length = 8;

            return input.Separate(sinfo.length, sinfo.depth, addPadding: isNumber ? true : false, resultAsPath: true);
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

        public static async Task<bool> TryCopyFileAsync(string sourcePath, string targetPath, int maxRetries = 5, int delayMilliseconds = 500, bool overwrite = true) {
            for (int attempt = 0; attempt < maxRetries; attempt++) {
                try {
                    File.Copy(sourcePath, targetPath, overwrite); // Overwrite = true
                    return true; // Success
                } catch (Exception) {
                    await Task.Delay(delayMilliseconds);
                }
            }
            return false;
        }

        public static bool PopulateVersionedPath(string dir_path,string file_basename, out string versionedPath) {
            file_basename = Path.GetFileName(file_basename);
            versionedPath = string.Empty;
            string pattern = $@"^{Regex.Escape(file_basename)}.##v(\d+)##$";
            var regex = new Regex(pattern,RegexOptions.IgnoreCase); //Case insensitive
            var files = Directory.GetFiles(dir_path, $"{file_basename}*");
            if (files.Length == 0 ) return false; //save as is. // There is no such file
            int maxversion = 0;
            foreach (var file in files) {
                var fname = Path.GetFileName(file);
                var match = regex.Match(fname);
                if (match.Success && int.TryParse(match.Groups[1].Value,out int version)) {
                    maxversion = Math.Max(maxversion, version); 
                }
            }
            maxversion++; //Get the version number.
            versionedPath =  Path.Combine(dir_path, $@"{file_basename}.##v{maxversion}##");
            return true;
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

        public static string BuildStoragePath(this IOSSRead input, string basePath, bool allowRootAccess = false, bool readonlyMode = false) {
            if (input == null || !(input is OSSReadRequest req)) throw new ArgumentNullException($@"{nameof(IOSSRead)} cannot be null. It has to be of type {nameof(OSSReadRequest)}");

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

        public static string BuildStoragePath(this List<OSSRoute> routes, string basePath, bool allow_root_access, bool readonlyMode) {

            string path = basePath;  
            if (!Directory.Exists(path)) throw new ArgumentException("BasePath Directory doesn't exists.");

            //Pull the lastone out.
            if (routes == null || routes.Count < 1) return path; //Directly create inside the basepath (applicable in few cases);
                                                                 //If one of the path is trying to make a root access, should we allow or deny?

            for (int i = 0; i < routes.Count; i++) { 

                //PATH PROCESSING
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

                //DIRECTORY PROCESSING & CREATION

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

            if (!path.StartsWith(basePath)) throw new ArgumentOutOfRangeException("The generated path is not accessible. Please check the inputs.");
            return path;
        }

        public static bool TryPopulateControlledGUID(this string value, out Guid result, OSSParseMode pmode = OSSParseMode.Parse, bool throwExceptions = false) {
            result = Guid.Empty;
            //Check if the value is already in the format of a hash.
            //This method is not responsible for removing the Hyphens, if found.
            if (string.IsNullOrWhiteSpace(value)) {
                if (throwExceptions) throw new ArgumentNullException("Unable to generate the GUID. The provided input is null or empty.");
                return false;
            }
            Guid guid;
            string workingValue = Path.GetFileNameWithoutExtension(value); //WITHOUT EXTENSION, ONLY FILE NAME
            switch (pmode) {
                case OSSParseMode.Parse:
                case OSSParseMode.ParseOrGenerate:
                    //Parse Mode : //Check if currently, the value is hashed or not.
                    
                    if (workingValue.IsValidGuid(out guid)) {
                    } else if (workingValue.IsCompactGuid(out guid)) {
                    } else if (pmode == OSSParseMode.ParseOrGenerate) {
                    guid = workingValue.ToDBName().CreateGUID(HashMethod.Sha256);
                    } else {
                        if (throwExceptions) throw new ArgumentNullException("Unable to generate the GUID. Please check the input.");
                        return false;
                    }
                break;
                case OSSParseMode.Generate:
                //Regardless of what is provided, we generate the hash based GUID.
                    guid = workingValue.ToDBName().CreateGUID(HashMethod.Sha256);
                break;
            }
            result = guid;
            return true;
        }
        public static bool TryPopulateControlledID(this string value, out long result, OSSParseMode pmode = OSSParseMode.Parse, Func<string,long> generator = null,bool throwExceptions = false) {
            result = 0;
            if (string.IsNullOrWhiteSpace(value)) {
                if (throwExceptions) throw new ArgumentNullException("Unable to generate the ID. The provided input is null or empty.");
                return false;
            }
            string workingValue = Path.GetFileNameWithoutExtension(value); //WITHOUT EXTENSION, ONLY FILE NAME
            switch (pmode) {
                case OSSParseMode.Parse:
                case OSSParseMode.ParseOrGenerate:
                    //For parse mode, we first try to parse.
                if (!long.TryParse(workingValue, out result)) {
                    //if it fails to parse, then check if we are allowed to generate.
                    //For parse mode also, we return false, For ParseOrGenerate if the generator is null, we return as well.

                    if (pmode == OSSParseMode.Parse || generator == null) {
                        if (throwExceptions) throw new ArgumentNullException($@"The provided input is not in the number format. Unable to parse a long value. ID Generator status : {generator != null}");
                        return false;
                    }
                    result = generator.Invoke(workingValue);
                }
                break;
                case OSSParseMode.Generate:
                if (generator == null) {
                    if (throwExceptions) throw new ArgumentNullException("Id Generator should be provided to fetch and generate ID");
                    return false;
                }
                
                result = generator.Invoke(workingValue);
                break;
            }
            if (result < 1) {
                if (throwExceptions) throw new ArgumentNullException("The final generated id is less than 1. Not acceptable. Please check the inputs.");
                return false;
            }
            return true;
        }
    }
}
