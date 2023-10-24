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

        //IF INCOMING CONFIG IS NULL, SEND AS IS. DO NOT TRY TO CREATE DEFAULT CONFIG HERE. IT WILL BE HANDLED IN INTERNAL REGISTRATION

        #region Tunneling Call
        public bool TryRegister<T>(T config, IConfigProvider<T> provider, List<IConfigConsumer<T>> consumers, bool replaceProviderIfExists = false, bool silentRegistration = true) where T : class, IConfig,new() {
            return RegisterInternal<T>(config, provider, null, replaceProviderIfExists, true).Result;
        }
        #endregion

        #region General Registrations
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
            throw new NotImplementedException();
        }

        public bool TryRegisterConsumer<T>(Action<T> action_consumer, out int id, bool silentRegistration = true) where T : class, IConfig,new() {
            throw new NotImplementedException();
        }

        public bool TryRegisterConsumers<T>(List<IConfigConsumer<T>> consumers, bool silentRegistration = true) where T : class, IConfig,new() {
            throw new NotImplementedException();
        }

        public bool TryRemoveConsumer<T>(IConfigConsumer<T> consumer) where T : class, IConfig,new() {
            throw new NotImplementedException();
        }
        public bool TryRemoveConsumer<T>(int actionId) where T :  IConfig {
            throw new NotImplementedException();
        }

        public bool TryRemoveConsumers<T>(List<IConfigConsumer<T>> consumers) where T : class, IConfig,new() {
            throw new NotImplementedException();
        }
        #endregion

        public bool TryRegisterOrUpdateProvider<T>(IConfigProvider<T> newProvider) where T : class, IConfig,new() {
            throw new NotImplementedException();
        }
    }
}