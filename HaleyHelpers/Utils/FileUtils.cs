using Haley.Enums;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Haley.Utils
{
    public static class FileUtils
    {
        private static readonly Regex KeyRegex = new(@"^[a-zA-Z0-9][a-zA-Z0-9\-_]*$", RegexOptions.Compiled);

        public static IReadOnlyDictionary<string, bool> ReadSeedConf(string filePath)
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!File.Exists(filePath)) return result; // Return empty if file doesn't exist

                foreach (var raw in File.ReadLines(filePath))
                {
                    var line = raw.Trim();

                    // ignore empty lines
                    if (string.IsNullOrEmpty(line)) continue;

                    // ignore seed headings / comment blocks
                    if (line.StartsWith("[") || line.StartsWith("##") || line.StartsWith("-")) continue;

                    // Decide enabled/disabled by leading '#'
                    bool enabled = true;
                    if (line.StartsWith("#")) {
                        enabled = false;
                        line = line.Substring(1).Trim(); // remove '#'
                    }

                    // Remove inline comment portion (anything after a '#')
                    int inlineHash = line.IndexOf('#');
                    if (inlineHash >= 0) line = line.Substring(inlineHash).Trim();

                    if (string.IsNullOrEmpty(line)) continue;

                    // Take the first token (in case there are extra words)
                    var key = line.CleanSplit(' ', '\t')[0];

                    // Ensure it's a "real" key line
                    if (!KeyRegex.IsMatch(key)) continue;

                    // If repeated, last one wins
                    result[key] = enabled;
                }

                return result;
            }
            catch (Exception)
            {
                return result;
            }
        }
    }
}