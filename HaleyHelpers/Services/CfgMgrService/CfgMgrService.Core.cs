using Haley.Abstractions;
using Haley.Enums;
using Haley.Internal;
using Haley.Models;
using Haley.Utils;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Haley.Services {

    public partial class ConfigManagerService : IConfigService {

        private async Task<T> GetDefaultConfig<T>() where T : class, IConfig, new() {
            try {
                //var dummyme = await GetMethodInfo(wrap.Provider.GetType(), "DummyMethod", wrap.ProviderExplicitName);
                //var res = wrap.Provider.InvokeMethod(dummyme, null);
                if (!GetWrapper<T>(out var wrap)) return null;
                var method = await GetMethodInfo(wrap.Provider.GetType(), ConfigMethods.ProviderPrepareDefault.MethodName(), wrap.ProviderExplicitName);
                return await wrap.Provider.InvokeMethod<T>(method); //We are expecting an output of type T
            } catch (Exception ex) {
                return new T(); //on failure, just create the default.
            }
        }

        private async Task<IConfig> GetDefaultConfig(ConfigWrapper wrap) {
            try {
                //var dummyme = await GetMethodInfo(wrap.Provider.GetType(), "DummyMethod", wrap.ProviderExplicitName);
                //var res = wrap.Provider.InvokeMethod(dummyme, null);
                var method = await GetMethodInfo(wrap.Provider.GetType(), ConfigMethods.ProviderPrepareDefault.MethodName(), wrap.ProviderExplicitName);
                return await wrap.Provider.InvokeMethod(method) as IConfig; //We are expecting an output of type T
            } catch (Exception ex) {
                HandleException(ex);
                return null;
            }
        }

        void SetExplicitNames<T>(ConfigWrapper wrap, bool forceUpdate = false) where T : class, IConfig, new() {

            if (wrap.Type != typeof(T)) throw new ArgumentException("Config Type inside the wrapper is not matching with the type of generic argument");
            //Get the explicit names to serach for the explicitly set interface names via reflection.
            if (string.IsNullOrWhiteSpace(wrap.ConsumerExplicitName) || forceUpdate) {
                wrap.SetExplicitConsumerName(typeof(IConfigConsumer<T>).FullName);
            }

            if (string.IsNullOrWhiteSpace(wrap.ProviderExplicitName) || forceUpdate) {
                wrap.SetExplicitProviderName(typeof(IConfigProvider<T>).FullName);
            }
        }

        private async Task<bool> RegisterInternal<T>(T config, IConfigProvider<T> provider, List<IConfigConsumer<T>> consumers, bool replaceProviderIfExists, bool silentRegistration) where T : class, IConfig, new() {
            //Convert incoming inputs into a Config Wrapper and deal internally.
            try {

                var configType = typeof(T);
                var configName = configType.FullName;
                bool firstEntry = !_configs.ContainsKey(configName); //Negate value.

                if (!GetWrapper<T>(out var wrap, true)) return false; //Create if not exists.

                SetExplicitNames<T>(wrap,false); //ForceUpdate is expensive.

                if (provider != null && (provider.UniqueId == null || provider.UniqueId == Guid.Empty)) {
                    provider.UniqueId = Guid.NewGuid();
                }

                //Process Provider and Config
                if (firstEntry || wrap.LoadPending) {
                    //For first registration, always give preference to loading config from the local directory, if not prepare default config.
                    if (config != null) {
                        wrap.Config = config; //Could be null as well.
                    } else {
                        //Try to load from directory. Even if that is empty, then in upcoming steps we will try to prepare default config.
                        if(LoadConfigFromDirectory(wrap, out var contents) && !string.IsNullOrWhiteSpace(contents)) {
                            wrap.Config = ConvertStringToConfig(contents, wrap.Type);
                            wrap.ConfigJsonData = contents;
                            this.ConfigLoaded?.Invoke(nameof(RegisterInternal), wrap.Type);
                        }
                    }

                    if (firstEntry || wrap.Provider == null) {
                        //Only replace for first entry but not for the loadpending calls.
                        wrap.Provider = provider;
                    }

                    if (wrap.LoadPending) {
                        wrap.LoadPending = false; //whatever the case, we reset it.
                    }
                } else if(replaceProviderIfExists && provider != null) {
                    //Sometimes, we might be calling this method directly for registering the consumers as well. In those cases, we should not replace the provider (which could be null)
                    wrap.Provider = provider; //Only replace provider, if the argument has been set.
                }

                //Handle Initial Config.
                if (wrap.Config == null && wrap.Provider != null) {
                    wrap.Config = await GetDefaultConfig<T>();
                }

                bool updateFailed = false;
                //Process Consumers.
                if (consumers != null) {
                    foreach (var consumer in consumers) {
                        if (consumer == null) continue; 
                        if (consumer.UniqueId == null || consumer.UniqueId == Guid.Empty) consumer.UniqueId = Guid.NewGuid();
                        var key = consumer.UniqueId.ToString();
                        if (!wrap.Consumers.ContainsKey(key)) { wrap.Consumers.TryAdd(key, null); }
                        wrap.Consumers[key] = consumer; //Add this consumer.
                        try {
                            if (!silentRegistration && wrap.Config != null) {
                                //Inform the consumers right away.
                                if (!await consumer.OnConfigChanged(wrap.Config as T)) {
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

        bool GetWrapper<T>(out ConfigWrapper wrapper,bool createIfNotExists = false) {
            wrapper = null;
            var configType = typeof(T);
            var configName = configType.FullName;
            bool firstEntry = !_configs.ContainsKey(configName); //Negate value.

            //Create and add new wrapper if not exists.
            if (firstEntry && createIfNotExists) {
                _configs.TryAdd(configName, new ConfigWrapper(configType));
            }

            if (!_configs.TryGetValue(configName, out ConfigWrapper wrap)) return false;
            wrapper = wrap;
            return true;
           
        }
    }
}