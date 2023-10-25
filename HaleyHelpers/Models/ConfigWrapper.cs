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
        string _explicitProviderFullName;
        string _explicitConsumerFullName;

        string _explicitProviderName;
        string _explicitConsumerName;

        public string StorageDirectory { get; set; }
        public Type Type { get; } //If config is null, we will not be able to ascertain the type.
        public string Name => Type?.Name; //This will also be the Generic Argument Name
        public string FullName => Type?.FullName;
        public object Provider { get; set; }

        public string ProviderExplicitName { get; set; }
        public string ConsumerExplicitName { get; set; }

        public void SetExplicitProviderName(string full_name) {
            try {
                _explicitProviderFullName = full_name;
                if (!string.IsNullOrWhiteSpace(full_name)) {
                    _explicitProviderName= full_name.Split('`')[0];
                    ProviderExplicitName = $@"{_explicitProviderName}<{FullName}>";
                }
            } catch (Exception) {

            }
        }
        public void SetExplicitConsumerName(string full_name) {
            try {
                _explicitConsumerFullName = full_name;
                if (!string.IsNullOrWhiteSpace(full_name)) {
                    _explicitConsumerName = full_name.Split('`')[0];
                    ConsumerExplicitName = $@"{_explicitConsumerName}<{FullName}>";
                }
            } catch (Exception) {

            }
        }
        public ConcurrentDictionary<string, object> ConsumerObjects { get; set; } = new ConcurrentDictionary<string, object>();
        public ConcurrentDictionary<int, Action<object>> ConsumerMethods { get; set; } = new ConcurrentDictionary<int, Action<object>>();
        public IConfig Config { get; internal set; } //Very important to internally set this. As this is a reference type, if we allow this to be publically set, then even consumers can directly update the values.
        internal string ConfigContents { get; set; }

        public ConfigWrapper(Type wrapperType) { Type = wrapperType; }
    }
}
