using Haley.Enums;
using Haley.Internal;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.Xml;

namespace Haley.Utils {

    public static class StorageUtils {

        public static void GenerateTargetName(this StorageRequest req) {
            //If we already have a targetName, we need not do anything else.
            if (!string.IsNullOrWhiteSpace(req.TargetName)) return;

            if (req.Preference == FileNamePreference.Number) {
                if (req.Source == FileNameSource.Id) {
                    req.TargetName = FetchId(req.Id, nameof(req.Id)).ToString();
                } else {
                    req.TargetName = FetchId(req.RawName, nameof(req.RawName)).ToString();
                }
            } else {
                if (req.Source == FileNameSource.Id) {
                    req.TargetName = FetchHash(req.Id, nameof(req.Id), req.ForcedHash);
                } else {
                    req.TargetName = FetchHash(req.RawName, nameof(req.RawName), req.ForcedHash);
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

        public static string GeneratePath(this StorageRequestBase req, string suffix) {
            if (string.IsNullOrWhiteSpace(req.TargetName)) throw new ArgumentException("TargetName is null. Cannot generate TargetPath");
            string fileNameFull = Path.GetFileNameWithoutExtension(req.TargetName);
            int dirDepth = fileNameFull.IsMD5() ? Constants.DIRDEPTH_HASH : Constants.DIRDEPTH_LONG;

            //Now split the fileNameFull into paths.
            var filePath = fileNameFull.SplitAsPath(Constants.CHARSPLITLENGTH, dirDepth);

            if (!string.IsNullOrWhiteSpace(suffix)) {
                filePath = filePath + suffix; //Add suffix D for directory and F for file
            }

            var extension = Path.GetExtension(req.TargetName);

            //Add extension if it is available.
            if (!string.IsNullOrWhiteSpace(extension)) {
                filePath = filePath + extension;
            }
            //Add Root directory info if present
            if (!string.IsNullOrWhiteSpace(req.RootDir)) {
                filePath = Path.Combine(req.RootDir, filePath);
            }
            return filePath?.ToLower();
        }

        internal static void GenerateTargetPath(this DiskStorageRequest req) {
            req.GenerateTargetName(); //Should fill the target name.
            req.TryGeneratePath(out var path);
            req.SetTargetPath(path);
        }

        internal static DiskStorageRequest ToDiskStorage(this StorageRequest input) {
            var dskReq = new DiskStorageRequest(input);
            dskReq?.GenerateTargetPath();
            return dskReq;
        }

        private static string FetchHash(string prop_value, string prop_name, bool forceHash) {
            //Check if the value is already in the format of a hash.
            //This method is not responsible for removing the Hyphens, if found.
            if (string.IsNullOrWhiteSpace(prop_value)) throw new ArgumentNullException($@"For selected Preference & Source : {prop_name} cannot be null");
            string workingValue = Path.GetFileNameWithoutExtension(prop_value);
            bool hashed = (!forceHash && workingValue.IsMD5());

            if (!hashed) workingValue = HashUtils.ComputeHash(workingValue.ToLowerInvariant(), HashMethod.MD5, false, true);
            if (!workingValue.IsMD5()) throw new ArgumentException($@"Unable to generate hash for the provided value {prop_value}");
            return workingValue.ToLower();
        }

        private static long FetchId(string prop_value, string prop_name) {
            if (string.IsNullOrWhiteSpace(prop_value)) throw new ArgumentNullException($@"For selected Preference & Source : {prop_name} cannot be null");
            string workingValue = Path.GetFileNameWithoutExtension(prop_value);
            if (!long.TryParse(workingValue, out var result)) throw new ArgumentException($@"For selected Preference & Source : {prop_value} is not valid");
            return result;
        }
    }
}