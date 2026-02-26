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
                    if (line.StartsWith("[") || line.StartsWith("#") || line.StartsWith("-")) continue;

                    // if it contains = 1 , true, =0 or no = is considered false.
                    bool enabled = false;

                    // Remove inline comment portion (anything after a '#')
                    int inlineHash = line.IndexOf('#');
                    if (inlineHash >= 0) line = line.Substring(inlineHash).Trim();

                    if (string.IsNullOrEmpty(line)) continue;

                    var items = line.CleanSplit('=');
                    if (items.Count() > 2) continue; //Something is wrong with this line of code.. We expect only a maxium of two components.
                    var key = items[0]; //get the key.
                    if (string.IsNullOrEmpty(key)) continue;
                    // Ensure it's a "real" key line
                    if (!KeyRegex.IsMatch(key)) continue;

                    do {
                        if (items.Length < 2) break;
                            //Second portion is present..  We need to determine, if it is true or false.
                            var value = items[1];
                        if (string.IsNullOrEmpty(value)) break;
                        enabled = value.ToBool();
                    } while (false);
            
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