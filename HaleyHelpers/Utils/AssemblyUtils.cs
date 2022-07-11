using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.IO;
using System.Reflection;
using Haley.Abstractions;
using Haley.Enums;
using Haley.Events;
using Haley.Models;
using Haley.Utils;
using System.Collections;
using System.Diagnostics;

namespace Haley.Utils
{
    public static class AssemblyUtils
    {
        private static AssemblyHelper helper = new AssemblyHelper();
        public static bool ForceLoadDependencies(AssemblyName[] reference_name_array) {
            try {
                helper.ForceLoadDependencies(reference_name_array);
                return true;
            }
            catch (Exception ex) {
                return false;
            }
        }

        public static Assembly OnAssemblyResolve(ResolveEventArgs args, DirectoryInfo directory, bool isreflectionOnlyLoad) {
            return helper.OnAssemblyResolve(args, directory, isreflectionOnlyLoad);
        }

        public static bool LoadAllAssemblies(string directoryPath, SearchOption option = SearchOption.TopDirectoryOnly) {
            return helper.LoadAllAssemblies(directoryPath, option);
        }
    }
}