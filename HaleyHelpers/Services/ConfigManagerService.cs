using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;
using Haley.Models;
using System.Collections.Concurrent;
using Haley.Abstractions;
using System.IO;
using System.Reflection;
using Haley.Utils;

namespace Haley.Services
{
    public class ConfigManagerService : IConfigManager
    {
        #region DELEGATES

        Func<IConfigInfo, string, string> PreSaveProcessor;
        Func<IConfigInfo, string, string> PostLoadProcessor;
        Func<IConfig, string> ConfigSerializer;
        Func<string, IConfig> ConfigDeserializer;

        #endregion

        #region ATTRIBUTES
        private const string DEFAULTEXTENSION = "json";
        object basePathObj = new object();
        string _basepath;
        ConcurrentDictionary<string,ConfigVault> _configs = new ConcurrentDictionary<string, ConfigVault>();

        #endregion

        #region PROPERTIES
        public bool UseCustomProcessors { get; set; }
        public bool UseCustomSerializers { get; set; }
        public string FileExtension { get; set; }
        #endregion

        #region EVENTS
        public event EventHandler<string> ConfigSaved;
        public event EventHandler<string> ConfigLoaded;
        #endregion

        #region CONSTRUCTORS
        public ConfigManagerService() { 
            UseCustomProcessors = true; 
            UseCustomSerializers = false; 
        }
        #endregion

        #region PUBLIC METHODS
        public string GetBasePath() {
            return EnsureBasePath(true);
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
        public bool TryRegister(IConfigInfo info, IConfig data,IConfigHandler handler, out IConfigInfo resultInfo) {
            resultInfo = null;
            if (info == null || string.IsNullOrWhiteSpace(info?.Name) || data == null) return false;
            //If the handler is null, then it's totally fine, we can always register handler later.

            if (_configs.TryGetValue(info?.Name?.ToLower(), out var vault)) {
                resultInfo = vault.Info; //assign the registered information
                if (vault.Handler == null) {
                    vault.Handler = handler;
                }
                return false; //already reigstered the key.
            }

            if (!RegisterInternal(info, data, handler)) return false;

            resultInfo = info;
            return true;
        }
        public bool TryRegister(string key, Type configurationType, IConfig data, IConfigHandler handler, out IConfigInfo resultInfo) {
            return TryRegister(new ConfigInfo(key.ToLower()).SetConfigType(configurationType), data,handler, out resultInfo);
        }

        public bool TryUpdateHandler(string key, IConfigHandler handler) {
            if (_configs.TryGetValue(key.ToLower(), out var vault)) {
                if (vault == null) return false;
                vault.Handler = handler; //Set this as the handler.
                return true;
            }
            return false;
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
            }
            catch (Exception ex) {
                return false;
            }
        }
        public void SaveAll() {
            foreach (var vault in _configs.Values) {
                try {
                    SaveInternal(vault);
                }
                catch (Exception) {
                    continue;
                }
            }
        }
        public string GetSavePath(IConfigInfo info) {
            EnsureBasePath();

            string finalPath = null;

            if (!string.IsNullOrWhiteSpace(info?.StorageDirectory)) {
                if (Path.IsPathRooted(info?.StorageDirectory)) {
                    finalPath = info?.StorageDirectory;
                }
                else {
                    finalPath = Path.Combine(_basepath, info?.StorageDirectory);
                }
            }

            if (finalPath == null) finalPath = _basepath;

            finalPath = Path.Combine(finalPath, $@"{info?.Name}.{(String.IsNullOrWhiteSpace(FileExtension)? DEFAULTEXTENSION : FileExtension)}"); //Attach extension.
            return finalPath;
        }
        public void SetBasePath(string base_path) {
            _basepath = base_path;
            EnsureBasePath();
        }
        public void SetProcessors(Func<IConfigInfo, string, string> presave_processor, Func<IConfigInfo, string, string> postload_processor) {
            PreSaveProcessor = presave_processor;
            PostLoadProcessor = postload_processor;
        }
        public void SetSerializer(Func<IConfig, string> serializer, Func<string, IConfig> deserializer) {
            ConfigSerializer = serializer;
            ConfigDeserializer = deserializer;
        }
        public async Task LoadAllConfig() {
            var _keys = _configs.Keys.ToList();
            //During runtime, it just loads the data from basepath.
            foreach (var key in _keys) {
                await LoadConfig(key);
            }
        }
        public async Task LoadConfig(string key) {
            if (_configs.TryGetValue(key.ToLower(), out var targetVault)) {
                if (targetVault?.Info == null) return;
                if (LoadConfig(targetVault, out var result)) {
                    targetVault.Config = result.data;

                    if (targetVault.Handler == null) return;
                    await targetVault.Handler.OnConfigLoaded(result.dataCopy);
                }
            }
        }
        public void ResetConfig(string key) {
            if (_configs.TryGetValue(key.ToLower(), out var vault)) {
                if (vault.Info == null) return;
                if (ResetConfig(vault, out var newData)) {
                    //UpdateConfig(key.ToLower(), newData);
                    vault.Config = newData; 
                    //targetRes.info.ChangeHandler.Invoke(ConfigStatus.Reset); //Just to avoid getting updated by the UpdateConfig method, we are sending in a new data.
                }
            }
        }

        public bool UpdateConfig(string key, IConfig config) {
            try {
                if (_configs.TryGetValue(key.ToLower(), out var vault)) {
                    //Dont' directly update the value of a value tuple (as it is VALUE tuple and not REFERENCE)
                    if (vault.Info == null) return false;
                    if (config.GetType() == vault.Info.ConfigType) {
                        //only types matches, then we udpate
                        vault.Config = config; //Since this is reference type, we directly change. (not a tupel)
                        //if(_configs.TryUpdate(key.ToLower(), (config, res.info), res)) {
                        //    return true;
                        //}
                        return true;
                    }
                }
                return false;
            }
            catch (Exception) {
                return false;
            }
        }
        #endregion

        #region PRIVATE METHODS
        private bool LoadConfig(ConfigVault vault, out (IConfig data,IConfig dataCopy) config) {
            config = (null,null);
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

                    IConfig newData = null,copyData = null;
                    if (UseCustomSerializers && ConfigDeserializer != null) {
                        newData = ConfigDeserializer.Invoke(contents);
                        copyData = ConfigDeserializer.Invoke(contents);
                    }
                    else {
                        newData = contents.JsonDeserialize(vault.Info.ConfigType) as IConfig;
                        copyData = contents.JsonDeserialize(vault.Info.ConfigType) as IConfig;
                    }
                    //Data and datacopy both will have two different unique ids.
                    config = (newData,copyData);
                    if (newData == null) break; //If data is null, do not load.
                    return true;
                } while (false);
                return false;
            }
            catch (Exception ex) {
                return false;
            }
        }
        private bool RegisterInternal(IConfigInfo info, IConfig data,IConfigHandler handler) {
            if (info?.ConfigType == null) throw new ArgumentException("ConfigType of the IConfigInfo cannot be null. Please provide a valid type that implements IConfiguration");
            if (info?.Name == null) {
                throw new ArgumentException("Config Name cannot be null. Please provide a valid name which will be used as the key");
            }
            //If already registered, 
            if (_configs.TryGetValue(info.Name.ToLower(),out var existVault)) {
                existVault.Handler = handler; //Only change the handler.
                return false;
            }

            var vault = new ConfigVault() { Handler = handler, Config = data, Info = info };

            if (_configs.TryAdd(info?.Name.ToLower(),vault )) {
                if (LoadConfig(vault, out var result)) {
                    //On fresh register we need not inform the 
                    //Update the config
                    UpdateConfig(info.Name.ToLower(), result.data);
                    handler.OnConfigLoaded(result.dataCopy);
                }
                return true;
            }
            return false;
        }
        private string EnsureBasePath(bool createDir = true) {
            lock (basePathObj) {
                if (string.IsNullOrWhiteSpace(_basepath)) {
                    //Use the EXE base path.
                    UriBuilder uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    _basepath = Path.Combine(Path.GetDirectoryName(path), "Configurations");
                }
            }
            if (!Directory.Exists(_basepath) && createDir) {
                Directory.CreateDirectory(_basepath);
            }
            return _basepath;
        }

        private bool SaveInternal(ConfigVault vault) {
            try {
                //First call the handler.
                if (vault.Handler != null) {
                    var updatedConfig = vault.Handler.GetUpdatedConfig();
                    if (updatedConfig != null) { 
                    vault.Config = updatedConfig; //Also save the internal info, so that we can fetch later
                    }
                }

                string finalPath = GetSavePath(vault.Info);
                string _json = String.Empty;

                if (UseCustomSerializers && ConfigSerializer != null) {
                    _json = ConfigSerializer.Invoke(vault.Config);
                }
                else {
                    _json = vault.Config.ToJson(); //Use internal extension method
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
            }
            catch (Exception ex) {
                return false;
            }
        }
        private bool ResetConfig(ConfigVault vault, out IConfig data) {
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
            }
            catch (Exception ex) {
                return false;
            }
        }
        #endregion
    }
}
