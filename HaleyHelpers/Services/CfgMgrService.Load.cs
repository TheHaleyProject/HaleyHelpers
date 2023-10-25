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

        public T GetConfig<T>() where T : class,IConfig,new() {
            if (!GetWrapper<T>(out var wrap)) return null;
            return wrap.Config as T; //Could be null as well.
        }

        public T GetConfigCopy<T>() where T : class,IConfig,new() {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateConfig<T>(T config) where T : class,IConfig,new() {
            throw new NotImplementedException();
        }

        public bool DeleteFile<T>() where T : class,IConfig,new() {
            throw new NotImplementedException();
        }

        public Task LoadConfig<T>() where T : class,IConfig,new() {
            throw new NotImplementedException();
        }

        public Task ResetConfig<T>() where T : class, IConfig,new() {
            throw new NotImplementedException();
        }
    }
}