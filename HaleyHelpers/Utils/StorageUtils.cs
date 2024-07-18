using Haley.Enums;
using Haley.Internal;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Haley.Utils {

    public static class StorageUtils {

        public static void GenerateTargetName(this StorageRequest req) {
            //If we already have a targetName, we need not do anything else.
            if (!string.IsNullOrWhiteSpace(req.TargetName)) return;

            if (req.Preference == StorageNamePreference.Number) {
                if (req.Source == StorageNameSource.Id) {
                    req.TargetName = FetchId(req.Id, nameof(req.Id)).ToString();
                } else {
                    req.TargetName = FetchId(req.RawName, nameof(req.RawName)).ToString();
                }
            } else {
                if (req.Source == StorageNameSource.Id) {
                    req.TargetName = FetchHash(req.Id, nameof(req.Id), req.HashMode);
                } else {
                    req.TargetName = FetchHash(req.RawName, nameof(req.RawName), req.HashMode);
                }
            }
            var extension = Path.GetExtension(req.RawName);
            if (!string.IsNullOrWhiteSpace(extension)) req.TargetName = req.TargetName + extension;
        }

        public static bool TryGeneratePath(this StorageRequestBase req,out string path, string suffix = null) {
            path = null;
            try {
                if (suffix == null) suffix = req.IsFolder ? "d" : "f";
                path = req.GeneratePath(suffix);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public static void SanitizeTargetName(this StorageRequest req) {
            if (string.IsNullOrWhiteSpace(req.TargetName)) return;
            if (req.Preference == StorageNamePreference.Number) {
                if (long.TryParse(Path.GetFileNameWithoutExtension(req.TargetName), out _)) return;
            } else {
                if (Path.GetFileNameWithoutExtension(req.TargetName).IsMD5()) return;
            }
            req.TargetName = null; 
        }


        public static string GeneratePath(this StorageRequestBase req, string suffix) {
            if (string.IsNullOrWhiteSpace(req.TargetName)) throw new ArgumentException("TargetName is null. Cannot generate TargetPath");
            string fileNameFull = Path.GetFileNameWithoutExtension(req.TargetName);
            int dirDepth = fileNameFull.IsMD5() ? Constants.DIRDEPTH_HASH : Constants.DIRDEPTH_LONG;

            //Now split the fileNameFull into paths.
            var pathResult = fileNameFull.SplitAsPath(Constants.CHARSPLITLENGTH, dirDepth,false,true);

            if (!string.IsNullOrWhiteSpace(suffix)) {
                pathResult = pathResult + suffix; //Add suffix D for directory and F for file
            }

            var extension = Path.GetExtension(req.TargetName);

            //Add extension if it is available. (only if it is not a folder)
            if (!string.IsNullOrWhiteSpace(extension) && !req.IsFolder) {
                pathResult = pathResult + extension;
            }
            //Add Root directory info if present
            if (!string.IsNullOrWhiteSpace(req.RootDir) && !string.IsNullOrWhiteSpace(pathResult)) {
                pathResult = Path.Combine(req.RootDir, pathResult);
            }
            return pathResult?.ToLower();
        }

        internal static void GenerateTargetPath(this DiskStorageRequest req) {
            req.GenerateTargetName(); //Should fill the target name.
            req.TryGeneratePath(out var path);
            req.SetTargetPath(path);
        }

        internal static DiskStorageRequest ToDiskStorage(this StorageRequest input) {
            var dskReq = new DiskStorageRequest(input);
            dskReq.SanitizeTargetName(); //Just to ensure we don't make any mistake.
            dskReq?.GenerateTargetPath();
            return dskReq;
        }

        private static string FetchHash(string prop_value, string prop_name, StorageNameHashMode hashmode) {
            //Check if the value is already in the format of a hash.
            //This method is not responsible for removing the Hyphens, if found.
            if (string.IsNullOrWhiteSpace(prop_value)) throw new ArgumentNullException($@"For selected Preference & Source : {prop_name} cannot be null");
            string workingValue = Path.GetFileNameWithoutExtension(prop_value);

            bool hashed = workingValue.IsMD5(); //Check if currently, the value is hashed or not.

            if (hashmode == StorageNameHashMode.Parse && !hashed) throw new ArgumentException($@"For selected Preference, Source & HashMode : {prop_value} is not a valid filename");

            if (!hashed || hashmode == StorageNameHashMode.Force) workingValue = HashUtils.ComputeHash(workingValue.ToLowerInvariant(), HashMethod.MD5, false, true);
            if (!workingValue.IsMD5()) throw new ArgumentException($@"Unable to generate hash for the provided value {prop_value}");
            return workingValue.ToLower();
        }

        private static long FetchId(string prop_value, string prop_name) {
            if (string.IsNullOrWhiteSpace(prop_value)) throw new ArgumentNullException($@"For selected Preference & Source : {prop_name} cannot be null");
            string workingValue = Path.GetFileNameWithoutExtension(prop_value);
            if (!long.TryParse(workingValue, out var result)) throw new ArgumentException($@"For selected Preference & Source : {prop_value} is not a valid filename");
            return result;
        }
    }
}