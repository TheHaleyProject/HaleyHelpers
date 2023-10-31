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
            try {
                if (assembly == null) assembly = Assembly.GetEntryAssembly();
                switch (info) {
                    case AssemblyInfo.Title:
                        return GetInfo<AssemblyTitleAttribute>(assembly);
                    case AssemblyInfo.Description:
                        return GetInfo<AssemblyDescriptionAttribute>(assembly);
                    case AssemblyInfo.Version:
                        return assembly.GetName().Version.ToString(); //send version directly
                    case AssemblyInfo.Product:
                        return GetInfo<AssemblyProductAttribute>(assembly);
                    case AssemblyInfo.Copyright:
                        return GetInfo<AssemblyCopyrightAttribute>(assembly);
                    case AssemblyInfo.Company:
                        return GetInfo<AssemblyCompanyAttribute>(assembly); 
                }
                return null;
            } catch (Exception) {
                return null;
            }
        }

        public static string GetBasePath (Assembly assembly = null) {
            try {
                if (assembly == null) assembly = Assembly.GetEntryAssembly(); //Not the executing assembly. Let us focus on the entry assembly or the main assembly.
                return new Uri(assembly.Location).LocalPath;
            } catch (Exception) {
                return null;
            }
        }

        public static string GetBaseDirectory(Assembly assembly = null) {
            try {
                var filepath = GetBasePath(assembly);
                if (filepath == null) return null;
                return Path.GetDirectoryName(filepath);
            } catch (Exception) {
                return null;
            }
        }

        public static string GetInfo<T>(Assembly assembly = null) where T : Attribute {
            try {
                if (assembly == null) assembly = Assembly.GetEntryAssembly();
                object[] attributes = assembly.GetCustomAttributes(typeof(T), false);
                if (attributes.Length > 0) {
                    T target_attribute = (T)attributes[0];
                    switch (typeof(T).Name) {
                        case nameof(AssemblyTitleAttribute):
                            var title = (target_attribute as AssemblyTitleAttribute)?.Title;
                            if (string.IsNullOrWhiteSpace(title)) {
                                title = Path.GetFileNameWithoutExtension(assembly.CodeBase);
                            }
                            return title; //incase title value is empty we get the dll name.
                        case nameof(AssemblyCompanyAttribute):
                            return (target_attribute as AssemblyCompanyAttribute)?.Company;
                        case nameof(AssemblyCopyrightAttribute):
                            return (target_attribute as AssemblyCopyrightAttribute)?.Copyright;
                        case nameof(AssemblyVersionAttribute):
                            return (target_attribute as AssemblyVersionAttribute)?.Version;
                        case nameof(AssemblyFileVersionAttribute):
                            return (target_attribute as AssemblyFileVersionAttribute)?.Version;
                        case nameof(AssemblyProductAttribute):
                            return (target_attribute as AssemblyProductAttribute)?.Product;
                        case nameof(AssemblyDescriptionAttribute):
                            return (target_attribute as AssemblyDescriptionAttribute)?.Description;
                        case nameof(AssemblyTrademarkAttribute):
                            return (target_attribute as AssemblyTrademarkAttribute)?.Trademark;
                    }
                }
                return null ;
            } catch (Exception) {
                return null;
            }
        }
    }
}