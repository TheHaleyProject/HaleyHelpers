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


        public IConfig GetConfig(string key) {
            if (_configs.TryGetValue(key?.ToLower(), out var result)) {
                return result.Config;
            }
            return null;
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

        public void SetProcessors(Func<Type, string, string> presave_processor, Func<Type, string, string> postload_processor) {
            _preSaveProcessor = presave_processor;
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

        

        private bool HandleException(Exception ex) {
            switch (ExceptionMode) {
                case ExceptionHandling.Throw:
                    throw ex;
                default:
                    Debug.WriteLine(ex);
                    return false;
            }
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
                    info.ConfigJsonData = contents;
                    info.Config = ConvertStringToConfig(contents, info.Type);
                    ////Update the config
                    //UpdateConfigInternal(info, ConvertStringToConfig(contents, info.Type)); //Store it in the file
                    ////Upon loading the internal data from local directory, we need to notify others.
                    if (!notifyConsumers) return true;
                    foreach (var consumerKvp in info.Consumers) {
                        try {
                            //typeof(DeclaringType).GetMethod("Linq").MakeGenericMethod(typeOne).Invoke(null, new object[] { Session });
                            var toShare = info.Config;
                            if (SendConfigCloneToConsumers) {
                                toShare = ConvertStringToConfig(contents, info.Type); //Generate a clone from the generated contents.
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