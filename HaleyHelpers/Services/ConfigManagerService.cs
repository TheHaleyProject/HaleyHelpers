using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace Haley.Services {

    public partial class ConfigManagerService : IConfigService {

        #region CONSTRUCTORS

        public ConfigManagerService() {
           
        }

        #endregion CONSTRUCTORS

        #region PUBLIC METHODS

        public void DeletaAllFiles() {
            foreach (var vault in _configs.Values) {
                try {
                    DeleteInternal(vault);
                } catch (Exception ex) {
                    switch (ExceptionMode) {
                        case ExceptionHandling.Throw:
                            throw;
                        default:
                            Debug.WriteLine(ex);
                            continue;
                    }
                }
            }
        }

        public bool DeleteFile(string key) {
            try {
                if (_configs.TryGetValue(key.ToLower(), out var vault)) {
                    //Save the config.
                    if (DeleteInternal(vault)) {
                        return true;
                    }
                }
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        public IEnumerable<IConfig> GetAllConfig() {
            return _configs.Values.Select(p => p.Config);
        }

        public string GetBasePath() {
            return EnsureBasePath(true);
        }

        public IConfig GetConfig(string key) {
            if (_configs.TryGetValue(key?.ToLower(), out var result)) {
                return result.Config;
            }
            return null;
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

            FileAttributes attr = File.GetAttributes(finalPath);
            if (!File.GetAttributes(finalPath).HasFlag(FileAttributes.Directory)) {
                finalPath = Path.GetDirectoryName(finalPath);
            }

            var filename = SaveWithFullName ? info.FullName : info.Name;
            finalPath = Path.Combine(finalPath, $@"{filename}.{(String.IsNullOrWhiteSpace(FileExtension) ? DEFAULTEXTENSION : FileExtension)}"); //Attach extension.
            return finalPath;
        }

        public async Task LoadAllConfig() {
            var _keys = _configs.Keys.ToList();
            //During runtime, it just loads the data from basepath.
            foreach (var key in _keys) {
                await LoadConfig(key);
            }
        }

        public async Task<bool> LoadConfig(string key) {
            //if (_configs.TryGetValue(key.ToLower(), out var targetVault)) {
            //    if (targetVault?.Info == null) return false;
            //    return await LoadConfigInternal(targetVault);
            //}
            return false;
        }

        public async Task ResetConfig(string key) {
            //if (_configs.TryGetValue(key.ToLower(), out var vault)) {
            //    if (vault.Info == null) return;
            //    if (ResetConfigInternal(vault, out var newData)) {
            //        //UpdateConfig(Key.ToLower(), newData);
            //        vault.Config = newData;
            //        //Whenever the config is reset, also inform clients
            //        await vault.Handler?.OnConfigUpdated(newData);
            //        //targetRes.info.ChangeHandler.Invoke(ConfigStatus.Reset); //Just to avoid getting updated by the UpdateConfig Method, we are sending in a new data.
            //    }
            //}
        }

        public bool Save(string key) {
            try {
                if (_configs.TryGetValue(key.ToLower(), out var vault)) {
                    //Save the config.
                    if (SaveInternal(vault)) {
                        return true;
                    }
                }
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        public void SaveAll() {
            foreach (var vault in _configs.Values) {
                try {
                    SaveInternal(vault);
                } catch (Exception ex) {
                    switch (ExceptionMode) {
                        case ExceptionHandling.Throw:
                            throw;
                        default:
                            Debug.WriteLine(ex);
                            continue;
                    }
                }
            }
        }

        public void SetBasePath(string base_path) {
            _basepath = base_path;
            EnsureBasePath();
        }

        public void SetProcessors(Func<Type, string, string> presave_processor, Func<Type, string, string> postload_processor) {
            _preLoadProcessor = presave_processor;
            _postLoadProcessor = postload_processor;
        }

        public void SetSerializer(Func<IConfig, string> serializer, Func<string, IConfig> deserializer) {
            _cfgSerializer = serializer;
            _cfgDeserializer = deserializer;
        }

        //public bool TryUpdateHandler(string key, IConfigHandler handler) {
        //    //if (key == null) return false;
        //    //if (_configs.TryGetValue(key.ToLower(), out var vault)) {
        //    //    if (vault == null) return false;
        //    //    vault.Handler = handler; //Set this as the handler.

        //    //    if (ReloadConfigOnHandlerUpdate) {
        //    //        return LoadConfigInternal(vault).Result;
        //    //    }
        //    //    return true;
        //    //}
        //    return false;
        //}

        public async Task<bool> UpdateConfig(string key, IConfig config) {
            //try {
            //    if (string.IsNullOrWhiteSpace(key)) return false;

            //    if (UpdateConfigInternal(key, config)) {
            //        //also notify handler that the config has been updated.
            //        if (_configs.TryGetValue(key.ToLower(), out var vault)) {
            //            await vault.Handler?.OnConfigUpdated(config);
            //        }
            return true;
            //    }
            //    return false;
            //} catch (Exception ex) {
            //    return HandleException(ex);
            //}
        }

        public IConfigService WithExceptionHandling(ExceptionHandling exceptionHandling) {
            ExceptionMode = exceptionHandling;
            return this;
        }

        private bool DeleteInternal(ConfigWrapper info) {
            try {
                string finalPath = GetSavePath(info);
                if (File.Exists(finalPath)) {
                    File.Delete(finalPath);
                }

                return true;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        private string GetConfigFileName(IConfig config) {
            if (config.FileName == null) return config.FileName;
            return config.GetType().FullName;
        }

        #endregion PUBLIC METHODS

        #region PRIVATE METHODS

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

        private bool HandleException(Exception ex) {
            switch (ExceptionMode) {
                case ExceptionHandling.Throw:
                    throw ex;
                default:
                    Debug.WriteLine(ex);
                    return false;
            }
        }

        private IConfig ConvertToConfig(string contents, Type configType) {
            IConfig data = null;
            try {
                if (UseCustomSerializers && _cfgDeserializer != null) {
                    data = _cfgDeserializer.Invoke(contents);
                } else {
                    data = contents.FromJson(configType) as IConfig;
                }
            } catch (Exception) {
            }
            return data;
        }

        private bool LoadConfigFromDirectory(ConfigWrapper wrapper, out string contents) {
            contents = string.Empty;
            try {
                if (wrapper == null) return false;
                //When an item is registere, also try to load already saved data.
                do {
                    //Load the file from the location and
                    string finalPath = GetSavePath(wrapper); //Load this file.
                    if (!File.Exists(finalPath)) break;
                    contents = File.ReadAllText(finalPath);
                    if (_postLoadProcessor != null && UseCustomProcessors) //this should be used by the config manager for any kind of encryption.
                    {
                        contents = _postLoadProcessor?.Invoke(wrapper.Type, contents);
                    }
                    if (contents == null) break;
                    return true;
                } while (false);
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        private async Task<bool> LoadConfigInternal(ConfigWrapper info, bool notifyConsumers = true) {
            try {
                if (info == null) return false;
                if (LoadConfigFromDirectory(info, out var contents)) {
                    //It is assumed that the incoming wrap is taken from the dictionary, so, it should be a reference.
                    //We can directly set the value.
                    info.ConfigContents = contents;
                    info.Config = ConvertToConfig(contents, info.Type);
                    ////Update the config
                    //UpdateConfigInternal(info, ConvertToConfig(contents, info.Type)); //Store it in the file
                    ////Upon loading the internal data from local directory, we need to notify others.
                    if (!notifyConsumers) return true;
                    foreach (var consumerKvp in info.ConsumerObjects) {
                        try {
                            //typeof(DeclaringType).GetMethod("Linq").MakeGenericMethod(typeOne).Invoke(null, new object[] { Session });
                            var toShare = info.Config;
                            if (SendConfigCloneToConsumers) {
                                toShare = ConvertToConfig(contents, info.Type); //Generate a clone from the generated contents.
                            }
                            var response = consumerKvp.Value.InvokeMethod<bool>("OnConfigUpdated", info.Type, toShare); 
                        } catch (Exception) {
                            continue;
                        }
                    }
                    return true;
                }
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        async Task<MethodInfo> GetMethodInfo(Type targetType,string methodName,string explicitMethodName = null,Type argsType = null) {
            try {
                //Prepare a key.
                string key = targetType.FullName + "##" + explicitMethodName ?? methodName + "##" + argsType?.Name ?? "NA";
                if (_methodCache.ContainsKey(key)) return _methodCache[key];
                var method = await ReflectionUtils.GetMethodInfo(targetType, methodName, explicitMethodName, argsType);
                _methodCache.TryAdd(key, method);
                return _methodCache[key];
            } catch (Exception) {
                return null;
            }
        }

        private async Task<bool> RegisterInternal<T>(T config, IConfigProvider<T> provider, List<IConfigConsumer<T>> consumers, bool replaceProviderIfExists, bool silentRegistration) where T:class,IConfig,new() {
            //Convert incoming inputs into a Config Wrapper and deal internally.
            try {

                var configType = typeof(T);
                var configName = configType.FullName;
                bool firstEntry = !_configs.ContainsKey(configName); //Negate value.

                //Create and add new wrapper if not exists.
                if (firstEntry) {
                    _configs.TryAdd(configName, new ConfigWrapper(configType));
                }

                if (!_configs.TryGetValue(configName, out ConfigWrapper wrap)) return false;

                //Get the explicit names.
                if (string.IsNullOrWhiteSpace(wrap.ConsumerExplicitName)) {
                    wrap.SetExplicitConsumerName(typeof(IConfigConsumer<T>).FullName);
                }

                if (string.IsNullOrWhiteSpace(wrap.ProviderExplicitName)) {
                    wrap.SetExplicitProviderName(typeof(IConfigProvider<T>).FullName);
                }

                if (provider != null && (provider.UniqueId == null || provider.UniqueId == Guid.Empty)) {
                    provider.UniqueId = Guid.NewGuid();
                }

                //Process Provider and Config
                if (firstEntry) {
                    //For first registration, always give preference to loading config from the local directory, if not prepare default config.
                    if (config != null) {
                        wrap.Config = config; //Could be null as well.
                    } else {
                        //Try to load from directory. Even if that is empty, then in upcoming steps we will try to prepare default config.
                    }
                    wrap.Provider = provider;
                } else if (replaceProviderIfExists) {
                    wrap.Provider = provider; //Only replace provider, if the argument has been set.
                }

                //Handle Initial Config.
                if (wrap.Config == null && wrap.Provider != null){
                    try {
                        var method = await GetMethodInfo(wrap.Provider.GetType(), nameof(provider.PrepareDefaultConfig), wrap.ProviderExplicitName);
                        wrap.Config = await wrap.Provider.InvokeMethod<T>(method);
                    } catch (Exception ex) {
                        wrap.Config = new T(); //on failure, just create the default.
                    }
                }

                bool updateFailed = false;
                //Process Consumers.
                if (consumers != null) {
                    foreach (var consumer in consumers) {
                        if (consumer.UniqueId == null || provider.UniqueId == Guid.Empty) consumer.UniqueId = Guid.NewGuid();
                        var key = consumer.UniqueId.ToString();
                        if (!wrap.ConsumerObjects.ContainsKey(key)) { wrap.ConsumerObjects.TryAdd(key, null); }
                        wrap.ConsumerObjects[key] = consumer; //Add this consumer.
                        try {
                            if (!silentRegistration) {
                                //Inform the consumers right away.
                                if (!await consumer.OnConfigUpdated(wrap.Config as T)) {
                                    updateFailed = true; //Even one failure will flag this up. Currently not used. May be used in future.
                                }; 
                            }
                        } catch (Exception) {
                            continue;
                        }
                    }
                }
                return true;
            } catch (Exception ex) {
                return false;
            }
        }

        private bool ResetConfigInternal(ConfigWrapper vault, out IConfig data) {
            data = null;
            try {
                //if (vault.Info == null) return false;
                ////When an item is registere, also try to load already saved data.
                //if (vault.Handler != null) {
                //    var defData = vault.Handler.PrepareDefaultConfig();
                //    if (defData == null) return false;
                //    data = defData; //Use this as the default data.
                //    return true;
                //}
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        private bool SaveInternal(ConfigWrapper vault) {
            try {
                ////First call the handler.
                //if (vault.Handler != null) {
                //    var updatedConfig = vault.Handler.FetchConfigToSave();
                //    if (updatedConfig != null) {
                //        vault.Config = updatedConfig; //Also save the internal info, so that we can fetch later
                //    }
                //}

                //string finalPath = GetSavePath(vault.Info);
                //string _json = String.Empty;

                //if (UseCustomSerializers && _cfgSerializer != null) {
                //    _json = _cfgSerializer.Invoke(vault.Config);
                //} else {
                //    _json = vault.Config.ToJson(); //Use internal extension Method
                //}
                //string tosaveJson = _json;

                //if (_preLoadProcessor != null && UseCustomProcessors) //this should be used by the config manager for any kind of encryption.
                //{
                //    tosaveJson = _preLoadProcessor?.Invoke(vault.Info, _json);
                //}

                //using (FileStream fs = File.Create(finalPath)) {
                //    byte[] fileinfo = new UTF8Encoding(true).GetBytes(tosaveJson);
                //    fs.Write(fileinfo, 0, fileinfo.Length);
                //}
                return true;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

    

        //private bool UpdateConfigInternal(ConfigWrapper info, IConfig config) {
        //    try {
        //        if (_configs.TryGetValue(key.ToLower(), out var vault)) {
        //            //Dont' directly update the value of a value tuple (as it is VALUE tuple and not REFERENCE)
        //            if (vault.Info == null) return false;
        //            if (config.GetType() == vault.Info.ConfigType) {
        //                //only types matches, then we udpate
        //                vault.Config = config; //Since this is reference type, we directly change. (not a tupel)
        //                //if(_configs.TryUpdate(Key.ToLower(), (config, res.info), res)) {
        //                //    return true;
        //                //}
        //                return true;
        //            }
        //        }
        //        return false;
        //    } catch (Exception ex) {
        //        return HandleException(ex);
        //    }
        //}

        #endregion PRIVATE METHODS
    }
}