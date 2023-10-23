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
    public class ConfigWrapper  {
        public string StorageDirectory { get; set; }
        public Type Type => Config?.GetType();
        public string Name => Type?.Name;
        public string FullName => Type?.FullName;
        public object Provider { get; set; }
        public ConcurrentDictionary<string, object> Consumers { get; set; } = new ConcurrentDictionary<string, object>();
        public IConfig Config { get; set; }
        public ConfigWrapper()
        {
        }
    }
}
