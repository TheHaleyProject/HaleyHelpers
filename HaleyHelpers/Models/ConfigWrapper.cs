using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Haley.Abstractions;
using System.Collections.Concurrent;
using System.Reflection;


namespace Haley.Models
{
    internal class ConfigWrapper  {
        public string StorageDirectory { get; set; }
        public Type Type { get; } //If config is null, we will not be able to ascertain the type.
        public string Name => Type?.Name;
        public string FullName => Type?.FullName;
        public object Provider { get; set; }
        public ConcurrentDictionary<string, object> ConsumerObjects { get; set; } = new ConcurrentDictionary<string, object>();
        public ConcurrentDictionary<int, Action<object>> ConsumerMethods { get; set; } = new ConcurrentDictionary<int, Action<object>>();
        public IConfig Config { get; internal set; } //Very important to internally set this. As this is a reference type, if we allow this to be publically set, then even consumers can directly update the values.
        internal string ConfigContents { get; set; }

        public ConfigWrapper(Type wrapperType) { Type = wrapperType; }
    }
}
