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
        Func<string, string> PreSaveProcessor;
        Func<string, string> PostLoadProcessor;
        private object basePathObj = new object();
        private string _basepath;
        private ConcurrentDictionary<string, (IConfig config, IConfigInfo info)> _configs = new ConcurrentDictionary<string, (IConfig, IConfigInfo)>();
        private ConcurrentDictionary<string, IConfigHandler> _handlers = new ConcurrentDictionary<string, IConfigHandler>();

        public ConfigManagerService() { }

        public string GetBasePath()
        {
            return EnsureBasePath(true);
        }

        public IEnumerable<IConfig> GetAllConfig()
        {
            return _configs.Values.Select(p => p.config);
        }

        public IConfig GetConfig(string key)
        {
            if (_configs.TryGetValue(key, out var result))
            {
                return result.config;
            }
            return null;
        }

        public IConfigInfo Register(IConfigInfo info, IConfig data)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.Name))
            {
                throw new ArgumentException("Info and Info.Name cannot be empty.");
            }

            if (data == null)
            {
                throw new ArgumentException("Data cannot be null. Please provide a valid data to register");
            }

            if (_configs.ContainsKey(info.Name))
            {
                throw new ArgumentException($@"A config data with same name {info.Name} is already registered. Provide an unique value.");
            }

            RegisterInternal(info, data);
            return info;
        }

        private bool LoadConfig(IConfigInfo info,out IConfig data)
        {
            data = null;
            try
            {
                //When an item is registere, also try to load already saved data.
                if (info.Handler != null)
                {
                    do
                    {
                        //Load the file from the location and 
                        string finalPath = GetSavePath(info); //Load this file.
                        if (!File.Exists(finalPath)) break;
                        string contents = File.ReadAllText(finalPath);
                        if (PostLoadProcessor != null) //this should be used by the config manager for any kind of encryption.
                        {
                            contents = PostLoadProcessor?.Invoke(contents);
                        }

                        data = contents.JsonDeserialize(info.ConfigType) as IConfig;
                        if (data == null) break; //If data is null, do not load.
                        info.Handler.UpdateConfig(data); //This should be used by the model to update its internal state.
                        return true;
                    } while (false);
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool RegisterInternal(IConfigInfo info, IConfig data)
        {
            if (info.ConfigType == null) throw new ArgumentException("ConfigType of the IConfigInfo cannot be null. Please provide a valid type that implements IConfiguration");

            if (_configs.TryAdd(info.Name, (data, info)))
            {
                if (LoadConfig(info, out var newData)) data = newData;
                return true;
            }
            return false;
        }

        public IConfigInfo Register(string key, Type configurationType, IConfig data,IConfigHandler handler)
        {
            return Register(new ConfigInfo(key,handler).SetConfigType(configurationType), data);
        }

        public bool TryRegister(IConfigInfo info, IConfig data, out IConfigInfo resultInfo)
        {
            resultInfo = null;
            if (info == null || string.IsNullOrWhiteSpace(info.Name)) return false;

            if (data == null) return false;

            if (_configs.ContainsKey(info.Name))
            {
                _configs.TryGetValue(info.Name, out var existingRes);
                resultInfo = existingRes.info;
                return false;
            }

            if (!RegisterInternal(info,data)) return false;

            resultInfo = info;
            return true;
        }
        public bool TryRegister(string key, Type configurationType, IConfig data, IConfigHandler handler, out IConfigInfo resultInfo)
        {
            return TryRegister(new ConfigInfo(key, handler).SetConfigType(configurationType), data,out resultInfo);
        }

        public void Save(string key)
        {
           if(_configs.TryGetValue(key,out var tosave))
            {
                //Save the config.
                Save(tosave.info, tosave.config);
            }
        }

        public void SaveAll()
        {
            foreach (var tosave in _configs.Values)
            {
                Save(tosave.info, tosave.config);
            }
        }

        private string EnsureBasePath(bool createDir = true)
        {
            lock (basePathObj)
            {
                if (string.IsNullOrWhiteSpace(_basepath))
                {
                    //Use the EXE base path.
                    UriBuilder uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
                    string path = Uri.UnescapeDataString(uri.Path);
                    _basepath = Path.Combine(Path.GetDirectoryName(path), "Configurations");
                }
            }
            if (!Directory.Exists(_basepath) && createDir)
            {
                Directory.CreateDirectory(_basepath);
            }
            return _basepath;
        }

        public string GetSavePath(IConfigInfo info)
        {
            EnsureBasePath();

            string finalPath = null;

            if (!string.IsNullOrWhiteSpace(info.StorageDirectory))
            {
                if (Path.IsPathRooted(info.StorageDirectory))
                {
                    finalPath = info.StorageDirectory;
                }
                else
                {
                    finalPath = Path.Combine(_basepath, info.StorageDirectory);
                }
            }

            if (finalPath == null) finalPath = _basepath;

            finalPath = Path.Combine(finalPath, info.Name + ".json"); //Attach extension.
            return finalPath;
        }

        private void Save(IConfigInfo info, IConfig data)
        {
            //First call the handler.
            if (info.Handler != null)
            {
                info.Handler.SaveConfig(); //This should save any in-memory cache data.
            }

            string finalPath = GetSavePath(info);
            var _json = data.ToJson();
            //if (data is PurgerConfig pData)
            //{ 
            //    var newjson = pData.ToJson(); 
            //}
            string tosaveJson = _json;

            if (PreSaveProcessor != null) //this should be used by the config manager for any kind of encryption.
            {
                tosaveJson = PreSaveProcessor?.Invoke(_json);
            }

            using (FileStream fs = File.Create(finalPath))
            {
                byte[] fileinfo = new UTF8Encoding(true).GetBytes(tosaveJson);
                fs.Write(fileinfo, 0, fileinfo.Length);
            }
        }


        public void SetBasePath(string base_path)
        {
            _basepath = base_path;
            EnsureBasePath();
        }

        public void SetProcessors(Func<string, string> presave_processor, Func<string, string> postload_processor)
        {
            PreSaveProcessor = presave_processor;
            PostLoadProcessor = postload_processor;
        }

        public void LoadAllConfig()
        {
            var _keys = _configs.Keys.ToList();
            //During runtime, it just loads the data from basepath.
            foreach (var key in _keys)
            {
                LoadConfig(key);
            }
        }

        public void LoadConfig(string key)
        {
            if (_configs.TryGetValue(key, out var targetRes))
            {
                if (LoadConfig(targetRes.info, out var newData))
                {
                    targetRes.config = newData;
                }
            }
        }
    }
}
