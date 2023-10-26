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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace Haley.Services {

    public partial class ConfigManagerService : IConfigService {

        public IEnumerable<IConfig> GetAllConfig(bool copy = true) {
            if (!copy) return _configs.Values.Select(p => p.Config);
            return _configs.Values.Select(p => GetConfigCopy(p));
        }

        public T GetConfig<T>(bool copy = true) where T : class, IConfig, new() {
            if (!GetWrapper<T>(out var wrap)) return null;
            if (!copy) return wrap.Config as T; //Could be null as well.
            return GetConfigCopy<T>(wrap);
        }

        private T GetConfigCopy<T>(ConfigWrapper wrap) where T : class,IConfig,new() {
            return GetConfigCopy(wrap) as T;
        }

        private IConfig GetConfigCopy(ConfigWrapper wrap) {
            if (wrap.Config == null) return null;
            if (string.IsNullOrWhiteSpace(wrap.ConfigJsonData)) {
                wrap.ConfigJsonData = ConvertConfigToString(wrap.Config, wrap.Type);
            }
            return ConvertStringToConfig(wrap.ConfigJsonData, wrap.Type); //This will create a copy
        }

        public async Task LoadAllConfig(bool loadParallely = true) {
            if (loadParallely) {
                Parallel.ForEach(_configs.Values, async (p) => await LoadConfig(p));
            } else {
                foreach (var _wrapper in _configs.Values) {
                    await LoadConfig(_wrapper);
                }
            }
        }

        public async Task LoadConfig<T>() where T : class, IConfig, new() {
            //Load from Directory, update 
            if (!GetWrapper<T>(out var wrap, true)) return; //We will create the wrapper if not exists.
            await LoadConfig(wrap);
            return;
        }

        public async Task<bool> UpdateConfig<T>(T config, bool notifyConsumers = false) where T : class, IConfig, new() {
            if (config == null) return false;
            if (!GetWrapper<T>(out var wrap)) return false;
            wrap.Config = config;
            wrap.ConfigJsonData = ConvertConfigToString(wrap.Config, wrap.Type);
            if (notifyConsumers) {
               await NotifyConsumers<T>(wrap);
            }
            return true;
        }

        private async Task<bool> LoadConfig(ConfigWrapper wrap, bool notifyConsumers = true) {
            try {
                if (wrap == null) return false;

                if (LoadConfigFromDirectory(wrap, out var contents)) {
                    //It is assumed that the incoming wrap is taken from the dictionary, so, it should be a reference.
                    //We can directly set the value.
                    wrap.ConfigJsonData = contents;
                    wrap.Config = ConvertStringToConfig(contents, wrap.Type);
                    this.ConfigLoaded?.Invoke(nameof(LoadConfig), wrap.Type);
                    ////Upon loading the internal data from local directory, we need to notify others.
                    if (!notifyConsumers) return true;
                    await NotifyConsumers(wrap);
                    return true;
                }
                return false;
            } catch (Exception ex) {
                return HandleException(ex);
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
    }
}