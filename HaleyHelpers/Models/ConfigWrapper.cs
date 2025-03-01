using System;
using Haley.Abstractions;
using System.Collections.Concurrent;

namespace Haley.Models
{
    internal class ConfigWrapper  {
        string _explicitConsumerFullName;
        string _explicitConsumerName;
        string _explicitProviderFullName;
        string _explicitProviderName;
        public ConfigWrapper(Type wrapperType) { Type = wrapperType; LoadPending = false; }

        public IConfig Config { get; internal set; }
        public string ConsumerExplicitName { get; set; }
        public ConcurrentDictionary<string, object> Consumers { get; set; } = new ConcurrentDictionary<string, object>();
        public string FullName => Type?.FullName;
        public string Name => Type?.Name;
        //This will also be the Generic Argument Name
        public object Provider { get; set; }
        public string ProviderExplicitName { get; set; }
        public string StorageDirectory { get; set; }
        public bool LoadPending { get; set; }
        public Type Type { get; } //If config is null, we will not be able to ascertain the type.
                                  //Very important to internally set this. As this is a reference type, if we allow this to be publically set, then even consumers can directly update the values.
        internal string ConfigJsonData { get; set; }

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
    }
}
