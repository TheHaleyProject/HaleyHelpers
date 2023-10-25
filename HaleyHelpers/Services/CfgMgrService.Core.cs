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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace Haley.Services {

    public partial class ConfigManagerService : IConfigService {

        private async Task<bool> RegisterInternal<T>(T config, IConfigProvider<T> provider, List<IConfigConsumer<T>> consumers, bool replaceProviderIfExists, bool silentRegistration) where T : class, IConfig, new() {
            //Convert incoming inputs into a Config Wrapper and deal internally.
            try {

                var configType = typeof(T);
                var configName = configType.FullName;
                bool firstEntry = !_configs.ContainsKey(configName); //Negate value.

                if (!GetWrapper<T>(out var wrap, true)) return false; //Create if not exists.

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
                } else if (replaceProviderIfExists && provider != null) {
                    //Sometimes, we might be calling this method directly for registering the consumers as well. In those cases, we should not replace the provider (which could be null)
                    wrap.Provider = provider; //Only replace provider, if the argument has been set.
                }

                //Handle Initial Config.
                if (wrap.Config == null && wrap.Provider != null) {
                    try {
                        //var dummyme = await GetMethodInfo(wrap.Provider.GetType(), "DummyMethod", wrap.ProviderExplicitName);
                        //var res = wrap.Provider.InvokeMethod(dummyme, null);

                        var method = await GetMethodInfo(wrap.Provider.GetType(), ConfigMethods.ProviderPrepareDefault.MethodName(), wrap.ProviderExplicitName);
                        wrap.Config = await wrap.Provider.InvokeMethod<T>(method); //We are expecting an output of type T
                    } catch (Exception ex) {
                        wrap.Config = new T(); //on failure, just create the default.
                    }
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

        async Task<MethodInfo> GetProviderMethodInfo(ConfigWrapper wrap,ConfigMethods method) {
            return await GetMethodInfo(wrap.Provider.GetType(), method.MethodName(), wrap.ProviderExplicitName);
        }

        MethodInfo GetConsumerMethodInfo(ConfigWrapper wrap, string consumerId,out object consumer) {
            consumer = null;
            if (string.IsNullOrWhiteSpace(consumerId)) return null;
            if (!wrap.Consumers.TryGetValue(consumerId, out var _consumer)) return null;
            consumer = _consumer; //Set this consumer.
            return GetMethodInfo(consumer.GetType(), ConfigMethods.ConsumerUpdateConfig.MethodName(), wrap.ConsumerExplicitName).Result;
        }

        MethodInfo GetConsumerMethodInfo(object consumer, string consumerExplicitName, Type argsType) {
            if (consumer == null) return null;
            return GetMethodInfo(consumer.GetType(), ConfigMethods.ConsumerUpdateConfig.MethodName(), consumerExplicitName,argsType).Result;
        }

        async Task<MethodInfo> GetMethodInfo(Type targetType, string methodName, string explicitInterfaceName = null, Type argsType = null) {
            try {
                //Prepare a key.
                var mName = methodName;
                if (!string.IsNullOrWhiteSpace(explicitInterfaceName)) {
                    mName = $@"{explicitInterfaceName}.{methodName}";
                }
                string key = targetType.FullName + "##" + mName + "##" + argsType?.Name ?? "NA";
                if (_methodCache.ContainsKey(key)) return _methodCache[key];
                var method = await ReflectionUtils.GetMethodInfo(targetType, methodName, explicitInterfaceName, argsType);
                if (method == null) return null; //Don't try to add any method.
                _methodCache.TryAdd(key, method);
                return _methodCache[key];
            } catch (Exception) {
                return null;
            }
        }
        private IConfig ConvertStringToConfig(string contents, Type configType) {
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