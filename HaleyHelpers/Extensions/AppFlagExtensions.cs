using Haley.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace Haley.Utils
{
    public static class AppFlagExtensions {
        public static bool LoadFromConfig(this AppFlags input, IConfigurationRoot manager = null) {
            try {
                if (manager == null) manager = ResourceUtils.GenerateConfigurationRoot();
                if (manager == null || manager["AppFlags"] == null) return false; //Dont' load anything.
                var flags = manager["AppFlags"].CleanSplit(',');
                if (flags == null || flags.Length < 1) return false;

                input.Debug = flags.Contains("debug", StringComparer.InvariantCultureIgnoreCase);
                input.Mock = flags.Contains("mock", StringComparer.InvariantCultureIgnoreCase) || !flags.Contains("no-mock", StringComparer.InvariantCultureIgnoreCase);
                input.ConsoleLog = flags.Contains("console", StringComparer.InvariantCultureIgnoreCase);
                input.ThrowExceptions = flags.Contains("throw", StringComparer.InvariantCultureIgnoreCase);
                input.DevEnvironment = ResourceUtils.IsDevelopment;
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
