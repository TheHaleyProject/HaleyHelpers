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

        #region DELEGATES

        private Func<Type,string, IConfig> _cfgDeserializer; //Custom serializer to Deserialize the string to Config
        private Func<Type,IConfig, string> _cfgSerializer; //Custom serializer to convert config to string
        private Func<Type, string, string> _postLoadProcessor;
        private Func<Type, string, string> _preSaveProcessor;

        #endregion DELEGATES

        #region ATTRIBUTES

        private const string DEFAULTEXTENSION = "json";
        private string _basepath;
        private object _basePathObj = new object();
        private ConcurrentDictionary<string, ConfigWrapper> _configs = new ConcurrentDictionary<string, ConfigWrapper>();

        #endregion ATTRIBUTES

        #region PROPERTIES

        public ExceptionHandling ExceptionMode { get; private set; } = ExceptionHandling.OutputDiagnostics;
        public string FileExtension { get; set; }
        public bool UseCustomProcessors { get; set; } = true;
        public bool UseCustomSerializers { get; set; } = false;
        public bool SaveWithFullName { get; set; } = false;
        //public bool SendConfigCloneToConsumers { get; set; } = false;

        #endregion PROPERTIES

        public EventHandler<Type> ConfigLoaded { get; set; }
        public EventHandler<Type> ConfigSaved { get; set; }

        static ConcurrentDictionary<string, MethodInfo> _methodCache = new ConcurrentDictionary<string, MethodInfo>();
    }
}