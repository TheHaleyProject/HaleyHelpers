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
using System.CodeDom;

namespace Haley.Utils
{
    public static class AssemblyUtils
    {
        //Reason for using a assembly helper is because, we need the assembly helper (which is a marshall object) to support with cross domain loading.
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

        public static string GetInfo(AssemblyInfo info,Assembly assembly = null) {
            if (assembly == null) assembly = Assembly.GetEntryAssembly();
            return assembly.GetInfo(info);
        }

        public static string GetBasePath (Assembly assembly = null) {
            if (assembly == null) assembly = Assembly.GetEntryAssembly(); //Not the executing assembly. Let us focus on the entry assembly or the main assembly.
            return assembly.GetBasePath();
        }

        public static string GetBaseDirectory(Assembly assembly = null,string parentFolder = null) {
            if (assembly == null) assembly = Assembly.GetEntryAssembly(); //Not the executing assembly. Let us focus on the entry assembly or the main assembly.
            return assembly.GetBaseDirectory(parentFolder);
        }
    }
}