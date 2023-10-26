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
        #region CONSTRUCTORS
        public ConfigManagerService() {
           
        }
        #endregion CONSTRUCTORS

        #region Delete Files
        public void DeleteAllFiles() {
            foreach (var wrapper in _configs.Values) {
                try {
                    DeleteInternal(wrapper);
                } catch (Exception ex) {
                    HandleException(ex);
                }
            }
        }
        public bool DeleteFile<T>() where T : class, IConfig, new() {
            if (!GetWrapper<T>(out var wrap)) return false;
            try {
                return DeleteInternal(wrap);
            } catch (Exception ex) {
                return HandleException(ex);
            }
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
        #endregion

        #region Reset
        public async Task ResetConfig<T>() where T : class, IConfig, new() {
            if (!GetWrapper<T>(out var wrap)) return;
            wrap.Config = await GetDefaultConfig<T>(); //First get the default config.
            if (wrap.Config != null) {
                wrap.ConfigJsonData = ConvertConfigToString(wrap.Config, wrap.Type);
                //Now notify all consumers.
                await NotifyConsumers(wrap);
            }
        }

        public async Task ResetAllConfig() {
            foreach (var wrap in _configs.Values) {
                try {
                    wrap.Config = await GetDefaultConfig(wrap); //First get the default config.
                    wrap.ConfigJsonData = ConvertConfigToString(wrap.Config, wrap.Type);
                    //Now notify all consumers.
                    await NotifyConsumers(wrap);
                } catch (Exception ex) {
                    HandleException(ex);
                    continue;
                }
            }
        }

        #endregion
        public void SetProcessors(Func<Type, string, string> presave_processor, Func<Type, string, string> postload_processor) {
            _preSaveProcessor = presave_processor;
            _postLoadProcessor = postload_processor;
        }

        public void SetSerializer(Func<Type,IConfig, string> serializer, Func<Type,string, IConfig> deserializer) {
            _cfgSerializer = serializer;
            _cfgDeserializer = deserializer;
        }

        public IConfigService WithExceptionHandling(ExceptionHandling exceptionHandling) {
            ExceptionMode = exceptionHandling;
            return this;
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
    }
}