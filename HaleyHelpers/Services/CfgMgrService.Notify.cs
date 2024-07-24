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
using static System.Collections.Specialized.BitVector32;

namespace Haley.Services {

    public partial class ConfigManagerService : IConfigService {

        public async Task NotifyConsumers<T>() where T : class, IConfig, new() {
            if (!GetWrapper<T>(out var wrap)) return;
            await NotifyConsumers<T>(wrap); //Here, the purpose of providing the generic argument is, it becomes easy to do a type cast and call the methods directly.
        }

        public async Task NotifyAllConsumers() {
            foreach (var wrap in _configs?.Values) {
                try {
                    await NotifyConsumers(wrap);
                } catch (Exception) {
                    continue;
                }
            }
        }

        async Task NotifyConsumers<T>(ConfigWrapper wrap) where T : class, IConfig, new() {
            try {
                if (wrap == null) return;
                if (wrap.Type != typeof(T)) throw new ArgumentException("Config Type inside the wrapper is not matching with the type of generic argument");
                await NotifyConsumersInternal(wrap, async (consumer) => {
                    try {
                        //The Config itself is a reference type object. So, sending direct value will result in undesired results, in case the user decides to make direct modification
                        //Send only a copy.
                        var toshare = ConvertStringToConfig(wrap.ConfigJsonData, wrap.Type) as T;
                        await (consumer as IConfigConsumer<T>).OnConfigChanged(toshare);
                    } catch (Exception ex) {
                        HandleException(ex);
                    }
                });
            } catch (Exception) {
                throw;
            }
        }

        async Task NotifyConsumers(ConfigWrapper wrap) {
            try {
                if (wrap == null) return;
               await NotifyConsumersInternal(wrap, async (consumer) => {
                   try {
                       var method = GetConsumerMethodInfo(consumer, wrap.ConsumerExplicitName, wrap.Type);
                       var toshare = ConvertStringToConfig(wrap.ConfigJsonData, wrap.Type);
                       await (consumer.InvokeMethod(method, toshare));
                   } catch (Exception ex) {
                       HandleException(ex);
                   }
               });
            } catch (Exception) {
                throw;
            }
        }

        async Task NotifyConsumersInternal(ConfigWrapper wrap, Action<object> notifyInvoker, bool parallely = false) {
            try {
                bool updateFailed = false;
                if (wrap == null || wrap.Consumers == null || notifyInvoker == null) return;

                if (parallely) {
                    Parallel.ForEach(wrap.Consumers.Values, (p) => notifyInvoker(p));
                } else {
                    foreach (var consumer in wrap.Consumers.Values) {
                        try {
                            if (consumer == null) continue;
                            notifyInvoker.BeginInvoke(consumer, null, null);
                        } catch (Exception) {
                            continue;
                        }
                    }
                }
            } catch (Exception) {
                throw;
            }
        }

        async Task<MethodInfo> GetProviderMethodInfo(ConfigWrapper wrap,ConfigMethods method) {
            return await GetMethodInfo(wrap.Provider.GetType(), method.MethodName(), wrap.ProviderExplicitName);
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
            if (string.IsNullOrWhiteSpace(contents)) return null;
            try {
                if (UseCustomSerializers && _cfgDeserializer != null) {
                    data = _cfgDeserializer.Invoke(configType, contents);
                } 
                if (data == null) {
                    data = contents.FromJson(configType) as IConfig;
                }
            } catch (Exception) {
            }
            return data;
        }

        private string ConvertConfigToString(IConfig config, Type configType) {
            string data = null;
            if (config == null) return string.Empty;
            try {
                if (UseCustomSerializers && _cfgDeserializer != null) {
                    data = _cfgSerializer.Invoke(configType, config);
                }
                if (data == null) {
                    data = config.ToJson();
                }
            } catch (Exception) {
            }
            return data;
        }
    }
}