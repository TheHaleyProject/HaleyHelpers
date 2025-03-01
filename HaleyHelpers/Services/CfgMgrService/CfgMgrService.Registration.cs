using Haley.Abstractions;
using Haley.Enums;
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
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Haley.Services {

    public partial class ConfigManagerService : IConfigService {

        //IF INCOMING CONFIG IS NULL, SEND AS IS. DO NOT TRY TO CREATE DEFAULT CONFIG HERE. IT WILL BE HANDLED IN INTERNAL REGISTRATION

        #region General Registrations

        #region Tunneling Call
        public bool TryRegister<T>(T config, IConfigProvider<T> provider, List<IConfigConsumer<T>> consumers, bool replaceProviderIfExists = false, bool silentRegistration = true) where T : class, IConfig, new() {
            return RegisterInternal<T>(config, provider, consumers, replaceProviderIfExists, silentRegistration).Result;
        }
        #endregion
        public bool TryRegister<T>(T config, IConfigProvider<T> provider, bool replaceProviderIfExists = false) where T : class, IConfig,new() {
            return TryRegister<T>(config, provider, null, replaceProviderIfExists);
        }

        public bool TryRegister<T>(IConfigProvider<T> provider, bool replaceProviderIfExists = false) where T : class, IConfig,new() {
            return TryRegister<T>(null, provider, replaceProviderIfExists);
        }

        public bool TryRegister<T>(IConfigProvider<T> provider, List<IConfigConsumer<T>> consumers, bool replaceProviderIfExists = false, bool silentRegistration = true) where T : class, IConfig,new() {
            return TryRegister<T>(null, provider, consumers, replaceProviderIfExists, silentRegistration);
        }

        public bool TryRegister<T>() where T : class, IConfig,new() {
            return TryRegister<T>(config: null);
        }

        public bool TryRegister<T>(T config) where T : class, IConfig,new() {
            return TryRegister<T>(config: config, provider: null);
        }
        #endregion

        #region Consumer
        public bool TryRegisterConsumer<T>(IConfigConsumer<T> consumer, bool silentRegistration = true) where T : class, IConfig,new() {
            return TryRegisterConsumers<T>(new List<IConfigConsumer<T>>() { consumer}, silentRegistration);
        }

        public bool TryRegisterConsumers<T>(List<IConfigConsumer<T>> consumers, bool silentRegistration = true) where T : class, IConfig,new() {
            return TryRegister<T>(null, null, consumers, silentRegistration: silentRegistration);
        }

        public bool TryRemoveConsumer<T>(IConfigConsumer<T> consumer) where T : class, IConfig,new() {
            return TryRemoveConsumers<T>(new List<IConfigConsumer<T>>() { consumer });
        }

        public bool TryRemoveConsumers<T>(List<IConfigConsumer<T>> consumers) where T : class, IConfig,new() {
            if (consumers == null || consumers.Count == 0) return true; //No input.
            if (GetWrapper<T>(out var wrapper) || wrapper.Consumers == null) return true; //unable to fetch value of key.

            var toremoveList = consumers.Where(p=> p.UniqueId != null && p.UniqueId != Guid.Empty).Select(q => q.UniqueId.ToString());

            foreach(var toremove in toremoveList) {
                wrapper.Consumers.TryRemove(toremove, out _);
            }
            return true; 
        }
        #endregion

        public bool TryRegisterOrUpdateProvider<T>(IConfigProvider<T> newProvider) where T : class, IConfig,new() {
            if (newProvider == null) return false;
            return TryRegister<T>(null, newProvider, null, true);
        }
    }
}