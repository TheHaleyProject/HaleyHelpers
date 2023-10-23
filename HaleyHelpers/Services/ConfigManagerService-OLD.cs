using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haley.Services {

    public class ConfigManagerService : IConfigService {

        #region DELEGATES

        private Func<string, IConfig> ConfigDeserializer;
        private Func<IConfig, string> ConfigSerializer;
        private Func<IConfigRegisterInfo, string, string> PostLoadProcessor;
        private Func<IConfigRegisterInfo, string, string> PreSaveProcessor;

        #endregion DELEGATES

        #region ATTRIBUTES

        private const string DEFAULTEXTENSION = "json";
        private string _basepath;
        private ConcurrentDictionary<string, ConcurrentDictionary<string, ConfigHandlerWrapper>> _configs = new ConcurrentDictionary<string, ConcurrentDictionary<string, ConfigHandlerWrapper>>();
        private object basePathObj = new object();

        #endregion ATTRIBUTES

        #region PROPERTIES

        public ExceptionHandling ExceptionMode { get; private set; }
        public string FileExtension { get; set; }
        public bool ReloadConfigOnHandlerUpdate { get; set; }
        public bool UseCustomProcessors { get; set; }
        public bool UseCustomSerializers { get; set; }

        #endregion PROPERTIES

        #region EVENTS

        //public event EventHandler<string> ConfigSaved;
        //public event EventHandler<string> ConfigLoaded;

        #endregion EVENTS

        #region CONSTRUCTORS

        public ConfigManagerService() {
            UseCustomProcessors = true;
            UseCustomSerializers = false;
            ReloadConfigOnHandlerUpdate = false;
            ExceptionMode = ExceptionHandling.OutputDiagnostics;
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

        public string GetSavePath(IConfigRegisterInfo info) {
            EnsureBasePath();

            string finalPath = null;

            if (!string.IsNullOrWhiteSpace(info?.StorageDirectory)) {
                if (Path.IsPathRooted(info?.StorageDirectory)) {
                    finalPath = info?.StorageDirectory;
                } else {
                    finalPath = Path.Combine(_basepath, info?.StorageDirectory);
                }
            }

            if (finalPath == null) finalPath = _basepath;

            finalPath = Path.Combine(finalPath, $@"{info?.Name}.{(String.IsNullOrWhiteSpace(FileExtension) ? DEFAULTEXTENSION : FileExtension)}"); //Attach extension.
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
            if (_configs.TryGetValue(key.ToLower(), out var targetVault)) {
                if (targetVault?.Info == null) return false;
                return await LoadConfigInternal(targetVault);
            }
            return false;
        }

        public async Task ResetConfig(string key) {
            if (_configs.TryGetValue(key.ToLower(), out var vault)) {
                if (vault.Info == null) return;
                if (ResetConfigInternal(vault, out var newData)) {
                    //UpdateConfig(Key.ToLower(), newData);
                    vault.Config = newData;
                    //Whenever the config is reset, also inform clients
                    await vault.Handler?.OnConfigLoaded(newData);
                    //targetRes.info.ChangeHandler.Invoke(ConfigStatus.Reset); //Just to avoid getting updated by the UpdateConfig Method, we are sending in a new data.
                }
            }
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

        public void SetProcessors(Func<IConfigRegisterInfo, string, string> presave_processor, Func<IConfigRegisterInfo, string, string> postload_processor) {
            PreSaveProcessor = presave_processor;
            PostLoadProcessor = postload_processor;
        }

        public void SetSerializer(Func<IConfig, string> serializer, Func<string, IConfig> deserializer) {
            ConfigSerializer = serializer;
            ConfigDeserializer = deserializer;
        }

        public bool TryRegister(IConfigRegisterInfo info, IConfig data, IConfigHandler handler, bool updateHandlerOnFailure) {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            //If the handler is null, then it's totally fine, we can always register handler later.
            return RegisterInternal(info, data, handler, updateHandlerOnFailure).Result;
        }

        public bool TryRegister(IConfig data, IConfigHandler handler, out IConfigRegisterInfo resultInfo, bool updateHandlerOnFailure = false) {
            return TryRegister(null, data, handler, out resultInfo, updateHandlerOnFailure);
        }

        public bool TryRegister(string key, IConfig data, IConfigHandler handler, out IConfigRegisterInfo resultInfo, bool updateHandlerOnFailure = false) {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (key == null) {
                key = GetConfigInfoKey(data, handler);
            }
            resultInfo = new ConfigInfo(key.ToLower()).SetConfigType(data.GetType());
            return TryRegister(resultInfo, data, handler, updateHandlerOnFailure);
        }

        public bool TryRegister<ConfigType>(IConfigHandler handler, out IConfigRegisterInfo resultInfo, bool updateHandlerOnFailure = false) where ConfigType : IConfig {
            //Instead of getting a name like random utils.. Try to generate the name out of the config type.

            return TryRegister<ConfigType>(null, handler, out resultInfo, updateHandlerOnFailure);
        }

        public bool TryRegister<ConfigType>(string key, IConfigHandler handler, out IConfigRegisterInfo resultInfo, bool updateHandlerOnFailure = false) where ConfigType : IConfig {
            if (key == null) key = GetConfigInfoKey<ConfigType>(handler);
            resultInfo = new ConfigInfo(key.ToLower()).SetConfigType(typeof(ConfigType));
            return TryRegister(resultInfo, null, handler, updateHandlerOnFailure);
        }

        public bool TryRegister(string key, IConfig data, IConfigHandler handler, bool updateHandlerOnFailure = false) {
            if (key == null) key = GetConfigInfoKey(data, handler);
            return TryRegister(key, data, handler, out _, updateHandlerOnFailure);
        }

        public bool TryRegister(IConfig data, IConfigHandler handler, bool updateHandlerOnFailure = false) {
            return TryRegister(data, handler, out _, updateHandlerOnFailure);
        }

        public bool TryRegister<ConfigType>(string key, IConfigHandler handler, bool updateHandlerOnFailure = false) where ConfigType : IConfig {
            return TryRegister<ConfigType>(key, handler, out _, updateHandlerOnFailure);
        }

        public bool TryRegister<ConfigType>(IConfigHandler handler, bool updateHandlerOnFailure = false) where ConfigType : IConfig {
            return TryRegister<ConfigType>(handler, out _, updateHandlerOnFailure);
        }

        public bool TryUpdateHandler(string key, IConfigHandler handler) {
            if (key == null) return false;
            if (_configs.TryGetValue(key.ToLower(), out var vault)) {
                if (vault == null) return false;
                vault.Handler = handler; //Set this as the handler.

                if (ReloadConfigOnHandlerUpdate) {
                    return LoadConfigInternal(vault).Result;
                }
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateConfig(string key, IConfig config) {
            try {
                if (string.IsNullOrWhiteSpace(key)) return false;

                if (UpdateConfigInternal(key, config)) {
                    //also notify handler that the config has been updated.
                    if (_configs.TryGetValue(key.ToLower(), out var vault)) {
                        await vault.Handler?.OnConfigLoaded(config);
                    }
                    return true;
                }
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        public IConfigService WithExceptionHandling(ExceptionHandling exceptionHandling) {
            ExceptionMode = exceptionHandling;
            return this;
        }

        private bool DeleteInternal(ConfigHandlerWrapper vault) {
            try {
                string finalPath = GetSavePath(vault.Info);
                if (File.Exists(finalPath)) {
                    File.Delete(finalPath);
                }

                return true;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        private string GetConfigInfoKey(IConfig config, IConfigHandler handler) {
            if (config == null || handler == null) return RandomUtils.GetString(64).SanitizeJWT();
            return GetConfigInfoKey(config.GetType(), handler.GetType());
        }

        private string GetConfigInfoKey<ConfigType>(IConfigHandler handler) where ConfigType : IConfig {
            if (handler == null) return RandomUtils.GetString(64).SanitizeJWT();
            return GetConfigInfoKey(typeof(ConfigType), handler.GetType());
        }

        private string GetConfigInfoKey(Type config, Type handler) {
            if (config == null || handler == null) return RandomUtils.GetString(64).SanitizeJWT();
            return $@"{handler.FullName}###{config.Name}"; //Only the handler gets full name.
        }

        #endregion PUBLIC METHODS

        #region PRIVATE METHODS

        public string GenerateKey<ConfigType>(IConfigHandler handler) where ConfigType : IConfig {
            return GetConfigInfoKey<ConfigType>(handler);
        }

        public string GenerateKey(Type configType, Type handlerType) {
            return GetConfigInfoKey(configType, handlerType);
        }

        private string EnsureBasePath(bool createDir = true) {
            lock (basePathObj) {
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

        private bool LoadConfigFromDirectory(ConfigHandlerWrapper vault, out (IConfig data, IConfig dataCopy) config) {
            config = (null, null);
            try {
                if (vault.Info == null) return false;
                //When an item is registere, also try to load already saved data.
                do {
                    //Load the file from the location and
                    string finalPath = GetSavePath(vault.Info); //Load this file.
                    if (!File.Exists(finalPath)) break;
                    string contents = File.ReadAllText(finalPath);
                    if (PostLoadProcessor != null && UseCustomProcessors) //this should be used by the config manager for any kind of encryption.
                    {
                        contents = PostLoadProcessor?.Invoke(vault.Info, contents);
                    }

                    IConfig newData = null, copyData = null;
                    if (UseCustomSerializers && ConfigDeserializer != null) {
                        newData = ConfigDeserializer.Invoke(contents);
                        copyData = ConfigDeserializer.Invoke(contents);
                    } else {
                        newData = contents.FromJson(vault.Info.ConfigType) as IConfig;
                        copyData = contents.FromJson(vault.Info.ConfigType) as IConfig;
                    }
                    //Data and datacopy both will have two different unique ids.
                    config = (newData, copyData);
                    if (newData == null) break; //If data is null, do not load.
                    return true;
                } while (false);
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        private async Task<bool> LoadConfigInternal(ConfigHandlerWrapper vault, string handlerId = null) {
            try {
                if (vault == null) return false;
                if (LoadConfigFromDirectory(vault, out var result)) {
                    //On fresh register we need not inform the
                    //Update the config
                    UpdateConfigInternal(vault.Info.Name.ToLower(), result.data); //just update, donot notify client.
                    //Now, notify either all clients, or only the client matching the handler Id.
                    if (!string.IsNullOrWhiteSpace(handlerId)) {
                        await vault.Handlers.FirstOrDefault(p => p.Key == handlerId).Value?.OnConfigLoaded(result.dataCopy);
                    } else {
                        foreach (var handlerKvp in vault.Handlers) {
                            try {
                                await handlerKvp.Value?.OnConfigLoaded(result.dataCopy);
                            } catch (Exception) {
                                continue;
                            }
                        }
                    }
                    return true;
                }
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        private async Task<bool> RegisterInternal(IConfigRegisterInfo info, IConfig data, IConfigHandler handler, bool updateHandlerOnFailure) {
            if (info == null) throw new ArgumentNullException("ConfigInfo");
            if (info?.ConfigType == null) throw new ArgumentException("ConfigType of the IConfigRegisterInfo cannot be null. Please provide a valid type that implements IConfiguration");
            if (info?.Name == null) {
                throw new ArgumentException("Config Name cannot be null. Please provide a valid name which will be used as the Key");
            }

            if (handler.UniqueId == null) handler.UniqueId = Guid.NewGuid(); //We are directly setting the unique ID on the handler. So, that next time, when this handler tries to make a call, it will send same id again.

            IConfig initialData = data;
            if (initialData == null) {
                initialData = handler?.PrepareDefaultConfig();
            }

            //First add the key if not present.
            if (!_configs.ContainsKey(info?.Name.ToLower())) {
                _configs.TryAdd(info?.Name.ToLower(), new ConcurrentDictionary<string, ConfigHandlerWrapper>());
            }

            //Get the current vault and add the handler.
            if (_configs.TryGetValue(info?.Name.ToLower(),out var handlerDic)){
                var handlerId = handler.UniqueId.ToString();
                if (!handlerDic.ContainsKey(handlerId)) {
                    handlerDic.TryAdd(handlerId, new ConfigHandlerWrapper() { Config = data, Handler = handler, Info = info });
                }

                if (handlerDic.TryGetValue(handlerId, out var currentWrapper)) {
                    currentWrapper.Handler = handler;
                    await LoadConfigInternal(currentWrapper, handlerId);
                }
               
                return true;
            }
            return false;
        }

        private bool ResetConfigInternal(ConfigHandlerWrapper vault, out IConfig data) {
            data = null;
            try {
                if (vault.Info == null) return false;
                //When an item is registere, also try to load already saved data.
                if (vault.Handler != null) {
                    var defData = vault.Handler.PrepareDefaultConfig();
                    if (defData == null) return false;
                    data = defData; //Use this as the default data.
                    return true;
                }
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        private bool SaveInternal(ConfigHandlerWrapper vault) {
            try {
                //First call the handler.
                if (vault.Handler != null) {
                    var updatedConfig = vault.Handler.OnConfigSaving();
                    if (updatedConfig != null) {
                        vault.Config = updatedConfig; //Also save the internal info, so that we can fetch later
                    }
                }

                string finalPath = GetSavePath(vault.Info);
                string _json = String.Empty;

                if (UseCustomSerializers && ConfigSerializer != null) {
                    _json = ConfigSerializer.Invoke(vault.Config);
                } else {
                    _json = vault.Config.ToJson(); //Use internal extension Method
                }
                string tosaveJson = _json;

                if (PreSaveProcessor != null && UseCustomProcessors) //this should be used by the config manager for any kind of encryption.
                {
                    tosaveJson = PreSaveProcessor?.Invoke(vault.Info, _json);
                }

                using (FileStream fs = File.Create(finalPath)) {
                    byte[] fileinfo = new UTF8Encoding(true).GetBytes(tosaveJson);
                    fs.Write(fileinfo, 0, fileinfo.Length);
                }
                return true;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        private bool UpdateConfigInternal(string key, IConfig config) {
            try {
                if (_configs.TryGetValue(key.ToLower(), out var vault)) {
                    //Dont' directly update the value of a value tuple (as it is VALUE tuple and not REFERENCE)
                    if (vault.Info == null) return false;
                    if (config.GetType() == vault.Info.ConfigType) {
                        //only types matches, then we udpate
                        vault.Config = config; //Since this is reference type, we directly change. (not a tupel)
                        //if(_configs.TryUpdate(Key.ToLower(), (config, res.info), res)) {
                        //    return true;
                        //}
                        return true;
                    }
                }
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
            }
        }

        #endregion PRIVATE METHODS
    }
}