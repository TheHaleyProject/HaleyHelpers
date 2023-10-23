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
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Haley.Services {

    public class ConfigManagerService : IConfigService {

        #region DELEGATES

        private Func<string, IConfig> _cfgDeserializer;
        private Func<IConfig, string> _cfgSerializer;
        private Func<Type, string, string> _postLoadProcessor;
        private Func<Type, string, string> _preLoadProcessor;

        #endregion DELEGATES

        #region ATTRIBUTES

        private const string DEFAULTEXTENSION = "json";
        private string _basepath;
        private object _basePathObj = new object();
        private ConcurrentDictionary<string, ConfigWrapper> _configs = new ConcurrentDictionary<string, ConfigWrapper>();

        #endregion ATTRIBUTES

        #region PROPERTIES

        public ExceptionHandling ExceptionMode { get; private set; }
        public string FileExtension { get; set; }
        public bool UseCustomProcessors { get; set; }
        public bool UseCustomSerializers { get; set; }
        public bool SaveWithFullName { get; set; }

        #endregion PROPERTIES

        #region EVENTS

        //public event EventHandler<string> ConfigSaved;
        //public event EventHandler<string> ConfigLoaded;

        #endregion EVENTS

        #region CONSTRUCTORS

        public ConfigManagerService() {
            UseCustomProcessors = true;
            UseCustomSerializers = false;
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

        public string GetSavePath(ConfigWrapper info) {
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

        public void SetProcessors(Func<Type, string, string> presave_processor, Func<Type, string, string> postload_processor) {
            _preLoadProcessor = presave_processor;
            _postLoadProcessor = postload_processor;
        }

        public void SetSerializer(Func<IConfig, string> serializer, Func<string, IConfig> deserializer) {
            _cfgSerializer = serializer;
            _cfgDeserializer = deserializer;
        }


        public bool TryRegister(IConfig data, IConfigHandler handler, bool updateHandlerOnFailure = false) {
            return TryRegister(data, handler, out _, updateHandlerOnFailure);
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

        private bool LoadConfigFromDirectory(ConfigWrapper wrapper, out (IConfig data, IConfig dataCopy) config) {
            config = (null, null);
            try {
                if (wrapper == null) return false;
                //When an item is registere, also try to load already saved data.
                do {
                    //Load the file from the location and
                    string finalPath = GetSavePath(wrapper); //Load this file.
                    if (!File.Exists(finalPath)) break;
                    string contents = File.ReadAllText(finalPath);
                    if (_postLoadProcessor != null && UseCustomProcessors) //this should be used by the config manager for any kind of encryption.
                    {
                        contents = _postLoadProcessor?.Invoke(wrapper.Type, contents);
                    }

                    IConfig newData = null, copyData = null;
                    if (UseCustomSerializers && _cfgDeserializer != null) {
                        newData = _cfgDeserializer.Invoke(contents);
                        copyData = _cfgDeserializer.Invoke(contents);
                    } else {
                        newData = contents.FromJson(wrapper.Type) as IConfig;
                        copyData = contents.FromJson(wrapper.Type) as IConfig;
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

        Task<T> GenerateAndCall<T>(object input,string method_name,Type argType,object argument) {
            if (input == null) throw new ArgumentNullException("input");
            input.GetType().GetMethod("OnConfigLoaded")?.MakeGenericMethod(argType)?.Invoke(input, new object[] { argument });
            return Task.FromResult(default(T));
        }

        private async Task<bool> LoadConfigInternal(ConfigWrapper info, bool notifyConsumers = true) {
            try {
                if (info == null) return false;
                if (LoadConfigFromDirectory(info, out var result)) {
                    //Update the config
                    UpdateConfigInternal(info, result.data);
                    //Upon loading the internal data from local directory, we need to notify others.
                    if (!notifyConsumers) return true;
                    foreach (var consumerKvp in info.Consumers) {
                        try {
                            //typeof(DeclaringType).GetMethod("Linq").MakeGenericMethod(typeOne).Invoke(null, new object[] { Session });
                           var response = await GenerateAndCall<bool>(consumerKvp.Value, "OnConfigLoaded", info.Type, result.dataCopy);
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

        private async Task<bool> RegisterInternal(ConfigWrapper info, IConfig data, object handler, bool updateHandlerOnFailure) {
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

            //Get the current wrapper and add the handler.
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

                if (UseCustomSerializers && _cfgSerializer != null) {
                    _json = _cfgSerializer.Invoke(vault.Config);
                } else {
                    _json = vault.Config.ToJson(); //Use internal extension Method
                }
                string tosaveJson = _json;

                if (_preLoadProcessor != null && UseCustomProcessors) //this should be used by the config manager for any kind of encryption.
                {
                    tosaveJson = _preLoadProcessor?.Invoke(vault.Info, _json);
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

        private bool UpdateConfigInternal(ConfigWrapper info, IConfig config) {
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

        public T GetConfig<T>() where T : IConfig {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateConfig<T>(T config) where T : IConfig {
            throw new NotImplementedException();
        }

        public IConfigService SetStorageDirectory<T>(string storageDirectory) where T : IConfig {
            throw new NotImplementedException();
        }

        public bool TryRegister<T>(T data, IConfigProvider<T> provider, bool replaceHandlerIfExists = false) where T : IConfig {
            throw new NotImplementedException();
        }

        public bool TryRegister<T>(IConfigProvider<T> provider, bool replaceHandlerIfExists = false) where T : IConfig {
            throw new NotImplementedException();
        }

        public bool TryUpdateProvider<T>(IConfigProvider<T> newProvider) where T : IConfig {
            throw new NotImplementedException();
        }

        public bool TryRegisterConsumer<T>(IConfigConsumer<T> consumer) where T : IConfig {
            throw new NotImplementedException();
        }

        public bool TryRemoveConsumer<T>(IConfigConsumer<T> consumer) where T : IConfig {
            throw new NotImplementedException();
        }

        public bool Save<T>() where T : IConfig {
            throw new NotImplementedException();
        }

        public bool DeleteFile<T>() where T : IConfig {
            throw new NotImplementedException();
        }

        public string GetSavePath<T>() where T : IConfig {
            throw new NotImplementedException();
        }

        public void SetProcessors(Func<Type, string, string> presave_processor, Func<Type, string, string> postload_processor) {
            throw new NotImplementedException();
        }

        public Task LoadConfig<T>() where T : IConfig {
            throw new NotImplementedException();
        }

        public Task ResetConfig<T>() {
            throw new NotImplementedException();
        }
    }
}