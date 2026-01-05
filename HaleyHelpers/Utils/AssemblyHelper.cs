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
    public class AssemblyHelper : MarshalByRefObject
    {
        public Assembly LoadAssembly(String assemblyPath, bool isReflectionOnly, bool loadAsBytes = true) {
            try {
                if (isReflectionOnly) {
                    return GetReflectionOnlyAssembly(assemblyPath, loadAsBytes);
                }
                else {
                    return GetExecutableAssembly(assemblyPath, loadAsBytes);
                }
            }
            catch (Exception ex) {
                //Log it at a later stage.
                // cannot be loaded in the new AppDomain.
                return null;
            }
        }
        public Assembly OnAssemblyResolve(ResolveEventArgs args, DirectoryInfo directory, bool isreflectionOnlyLoad) {
            //Below are only for loading the dll's which are not resolved. So, no need to load via the bytes. We can directly load the dll.
            try {
                Assembly loadedAssembly = null;
                Func<Assembly, bool> getAssemblyPredicate = (Assembly asm) => { return string.Equals(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase); };

                //Case 1 : If it exists already in the Domain, return it.
                if (isreflectionOnlyLoad) {
                    loadedAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(
              asm => getAssemblyPredicate(asm));
                }
                else {
                    loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
              asm => getAssemblyPredicate(asm));
                }

                if (loadedAssembly != null) {
                    return loadedAssembly;
                }

                //Case 2: Doesn't exist in domain.So, check if the file exists.
                AssemblyName assemblyName = new AssemblyName(args.Name);
                string dependentAssemblyFilename = Path.Combine(directory.FullName, assemblyName.Name + ".dll");
                if (File.Exists(dependentAssemblyFilename)) {
                    if (isreflectionOnlyLoad) {
                        return Assembly.ReflectionOnlyLoadFrom(dependentAssemblyFilename);
                    }
                    else {
                        return Assembly.LoadFrom(dependentAssemblyFilename); //Not loading the bytes.
                    }
                }
                return null;
                //return Assembly.ReflectionOnlyLoad(args.Name);
            }
            catch (Exception ex) {
                throw ex;
            }
        }
        public void InitReflectionContext() {
            //Load all the assemblies that are already loaded in the current domain in to reflection context also.
            var assemblies = (from Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()
                              where !(assembly is System.Reflection.Emit.AssemblyBuilder)
                              && assembly.GetType().FullName != "System.Reflection.Emit.InternalAssemblyBuilder"
                              && !String.IsNullOrEmpty(assembly.Location)
                              select assembly
                                ).ToList();
            List<Assembly> loaded_assemblies = new List<Assembly>(); //Just for reference.
            foreach (var assembly in assemblies) {
                try {
                    //If the assembly is already loaded from reflection, ignore it.
                    if (loaded_assemblies.Any(p => p.FullName == assembly.FullName)) continue;
                    var loaded_assembly = Assembly.ReflectionOnlyLoadFrom(assembly.Location);
                    loaded_assemblies.Add(loaded_assembly);
                }
                catch (Exception ex) {
                    //log it.
                    continue; //Don't throw error at the moment.
                }
            }
        }
        public Assembly GetReflectionOnlyAssembly(string assemblyFullPath, bool loadAsBytes) {
            //ReflectiononlyLoad DOES NOT load the assembly into AppDomain (GAC)
            //We are merely trying to load the assembly to check if we are missing any dependencies and if so, we will try to load it from the path.
            try {
                if (loadAsBytes) {
                    return Assembly.ReflectionOnlyLoad(File.ReadAllBytes(assemblyFullPath));
                }
                else {
                    return Assembly.ReflectionOnlyLoadFrom(assemblyFullPath);
                }
            }
            catch (Exception ex) {
                //For reflection load errors, return true
                return null;
            }
        }
        public Assembly GetReflectionOnlyAssembly(string assemblyFullPath) {
            return GetReflectionOnlyAssembly(assemblyFullPath, true);
        }
        public Assembly GetExecutableAssembly(string assemblyFullPath, bool loadAsBytes) {
            try {
                if (loadAsBytes) {
                    return Assembly.Load(File.ReadAllBytes(assemblyFullPath)); //This assembly is loaded in to the memory and will never be unloaded until the application is closed.
                }
                else {
                    return Assembly.LoadFrom(assemblyFullPath);
                }
            }
            catch (Exception ex) {
                //For reflection load errors, return true
                return null;
            }
        }
        public Assembly GetExecutableAssembly(string assemblyFullPath) {
            return GetExecutableAssembly(assemblyFullPath, true);
        }
        public IEnumerable<Type> GetExportedTypes(Assembly assembly, string typeName, bool ignoreExceptions = false) {
            Type[] exportedTypes = assembly?.GetExportedTypes(); //Get all the exported types.

            Func<Type, bool> interfaceCheck = (Type inType) => {
                return inType.GetInterfaces().Any(q => q.AssemblyQualifiedName == typeName);
            };

            return exportedTypes.Where(exType => interfaceCheck.Invoke(exType) && exType.Name != typeName && !exType.IsAbstract);
        }

        public void ForceLoadDependencies(AssemblyName[] reference_name_array) {
            try {
                //AssemblyName[] reference_name_array = Assembly.GetExecutingAssembly().GetReferencedAssemblies(); //Force load all referenced assemblies into memory.
                foreach (var assname in reference_name_array) {
                    List<Assembly> loaded_assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
                    if (!(loaded_assemblies.Any(p => p.GetName().Name == assname.Name))) {
                        Assembly.Load(assname);
                    }
                }
            }
            catch (Exception) {
                throw;
            }
        }

        public bool LoadAllAssemblies(string directoryPath, SearchOption option = SearchOption.TopDirectoryOnly) {
            try {
                if (!Directory.Exists(directoryPath)) return false;
                var allFiles = Directory.GetFiles(directoryPath, "*.dll", option);
                foreach (var assname in allFiles) {
                    List<Assembly> loaded_assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
                    //var loaded_paths = loaded_assemblies.Select(a => a.Location).ToArray(); //What if the assembly is loaded by Byte[]? then we won't have path?
                    var asmName = Path.GetFileNameWithoutExtension(assname);

                    if (!(loaded_assemblies.Any(p => p.GetName().Name.Equals(asmName, StringComparison.OrdinalIgnoreCase)))) {
                        try {
                            Assembly.LoadFrom(assname);
                        }
                        catch (Exception ex) {
                            continue;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex) {
                return false;
            }
        }
    }
}
