using Haley.Abstractions;
using Haley.Enums;
using Haley.Internal;
using Haley.Models;
using Haley.Utils;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace Haley.Services {

    public partial class ConfigManagerService : IConfigService {

        public string GetBasePath() {
            return EnsureBasePath(true);
        }

        public string GetSavePath<T>() where T : class, IConfig, new() {
            if (!GetWrapper<T>(out var wrap)) return null;
            return GetSavePath(wrap);
        }

        public async Task<bool> Save<T>(bool notifyConsumers = true,bool writeToDirectory = true, bool askProvider = true) where T : class, IConfig, new() {
            if (!GetWrapper<T>(out var wrap)) return false;
            return await SaveInternal(wrap,notifyConsumers,writeToDirectory,askProvider);
        }

        public async Task SaveAll(bool notifyConsumers = true,bool writeToDirectory = true, bool askProvider = true) {
            Parallel.ForEach(_configs.Values, async (w) => await SaveInternal(w, notifyConsumers, writeToDirectory, askProvider));
        }

        public void SetBasePath(string base_path) {
            _basepath = base_path;

            if (!string.IsNullOrWhiteSpace(base_path)) {
                FileAttributes attr = File.GetAttributes(base_path);
                if (!attr.HasFlag(FileAttributes.Directory)) {
                    _basepath = Path.GetDirectoryName(base_path); //Just get the base path of the provided value.
                }
            }
            EnsureBasePath();
        }

        public IConfigService SetStorageDirectory<T>(string storageDirectory) where T : class, IConfig, new() {
            if (GetWrapper<T>(out var wrap)) {
                wrap.StorageDirectory = storageDirectory; //should be a path. Could be null as well, if user decides to reset.
                //if the storage directory is a file name, then reset it
                if (!string.IsNullOrWhiteSpace(wrap.StorageDirectory)) {
                    FileAttributes attr = File.GetAttributes(wrap.StorageDirectory);
                    if (!attr.HasFlag(FileAttributes.Directory)) {
                        wrap.StorageDirectory = Path.GetDirectoryName(wrap.StorageDirectory); //Just get the base path of the provided value.
                    }
                }
            }
            return this;
        }
        private string EnsureBasePath(bool createDir = true) {
            lock (_basePathObj) {
                if (string.IsNullOrWhiteSpace(_basepath)) {
                    _basepath = Path.Combine(AssemblyUtils.GetBaseDirectory(), "Configurations");
                }
            }
            if (!Directory.Exists(_basepath) && createDir) {
                Directory.CreateDirectory(_basepath);
            }
            return _basepath;
        }

        string GetConfigJsonData(IConfig config, Type configType = null) {
            string jsonContent = string.Empty;
            try {
                if (config == null) return jsonContent;

                if (UseCustomSerializers && _cfgSerializer != null) {
                    jsonContent = _cfgSerializer.Invoke(configType, config);
                }

                if (string.IsNullOrWhiteSpace(jsonContent)) {
                    jsonContent = config.ToJson(); //Use internal extension Method
                }
                return jsonContent;
            } catch (Exception) {
                return string.Empty;
            }
        }

        string GetSavePath(ConfigWrapper info) {
            EnsureBasePath();

            string finalPath = null;
            if (info == null) return null;
            if (!string.IsNullOrWhiteSpace(info.StorageDirectory)) {
                if (Path.IsPathRooted(info.StorageDirectory)) {
                    finalPath = info.StorageDirectory;
                } else {
                    finalPath = Path.Combine(_basepath, info.StorageDirectory);
                }
            }

            if (finalPath == null) finalPath = _basepath;

            //FileAttributes attr = File.GetAttributes(finalPath);
            //if (!File.GetAttributes(finalPath).HasFlag(FileAttributes.Directory)) {
            //    finalPath = Path.GetDirectoryName(finalPath);
            //}

            var filename = SaveWithFullName ? info.FullName : info.Name;
            finalPath = Path.Combine(finalPath, $@"{filename}.{(String.IsNullOrWhiteSpace(FileExtension) ? DEFAULTEXTENSION : FileExtension)}"); //Attach extension.
            return finalPath;
        }

        private async Task<bool> SaveInternal(ConfigWrapper wrap,bool notifyConsumers = true, bool writeToDirectory = true, bool askProvider = true) {
            try {
                IConfig cfgToSave = null;

                //Decide if you want to ask from provider or a silent save.
                if (askProvider) {
                    //From the wrap, get the provider and ask for updated config upon saving. If that seems to be null
                    var mInfo = await GetProviderMethodInfo(wrap, ConfigMethods.ProviderOnSaving);
                    if (mInfo == null) return false;
                    var toSave = await wrap.Provider.InvokeMethod(mInfo); //Now take this config and update internal and save to directory.
                    if (toSave == null) return false;
                    
                    if (!(toSave is IConfig _cfgToSave)) {
                        //todo: Should we delete the local file?? may be the null return was intentional.
                        return false;
                    }

                    cfgToSave = _cfgToSave;
                } else {
                    cfgToSave = wrap.Config; //already existing data.
                }
             
                if (cfgToSave == null) return false;
                string tosaveJson = GetConfigJsonData(cfgToSave);
                //This serialized json, save it back to 
                if (string.IsNullOrWhiteSpace(tosaveJson)) return false;

                //First save it back to the wrapper.
                wrap.Config = cfgToSave;
                wrap.ConfigJsonData = tosaveJson;

                if (writeToDirectory) {
                    WriteToDirectory(tosaveJson, wrap);
                }

                //Rise the event first.

                if(notifyConsumers && wrap.Consumers != null) {
                   await NotifyConsumers(wrap);
                }
                //Once we properly saved it to the local file, we notify other places.
                return true;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        bool WriteToDirectory(string input, ConfigWrapper wrap) {
            try {
                string tosaveJson = input;
                if (string.IsNullOrWhiteSpace(input) || wrap == null) return false;
                if (_preSaveProcessor != null && UseCustomProcessors) //this should be used by the config manager for any kind of encryption.
                    {
                    tosaveJson = _preSaveProcessor?.Invoke(wrap.Type, input); // may be the data is analyzed and removed of some value. or completely encryped as well.
                }

                string finalPath = GetSavePath(wrap);
                using (FileStream fs = File.Create(finalPath)) {
                    byte[] fileinfo = new UTF8Encoding(true).GetBytes(tosaveJson);
                    fs.Write(fileinfo, 0, fileinfo.Length);
                }
                return true;
            } catch (Exception ex) {
                return false;
            }
            
        }
    }
}