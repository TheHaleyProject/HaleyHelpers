using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Haley.Utils
{
    public static class DirectoryUtils {
        public static async Task<bool> TryReplaceFileAsync(this Stream sourceStream, string path, int bufferSize, int maxRetries = 5, int delayMilliseconds = 500) {
            for (int attempt = 0; attempt < maxRetries; attempt++) {
                try {
                    if (!sourceStream.CanSeek && sourceStream.Position > 0) return false;
                    sourceStream.Position = 0; // Reset the source stream position if it is seekable
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        await sourceStream.CopyToAsync(fs, bufferSize);
                        return true;
                    }
                } catch (IOException ex) {
                    await TryDeleteFile(path, 2);
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
            var extension = Path.GetExtension(file_basename);

            if (string.IsNullOrWhiteSpace(extension)) {
                extension = "unknown";
            }else {
                extension.TrimStart('.');
            }

            versionedPath = string.Empty;
            string pattern = $@"^{Regex.Escape(file_basename)}.##v(\d+)##.{extension}$";
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
            versionedPath =  Path.Combine(dir_path, $@"{file_basename}.##v{maxversion}##.{extension}");
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
                } catch (UnauthorizedAccessException) {
                    await Task.Delay(delayMilliseconds);
                }
            }
            return false;
        }
    }
}
