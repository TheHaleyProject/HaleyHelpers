using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Internal {
    internal static class HelperExtensions {
        public static string MethodName(this ConfigMethods method) {
            string methodName = string.Empty;
            switch (method) {

                case ConfigMethods.ConsumerUpdateConfig:
                    methodName = nameof(IConfigConsumer<ConfigBase>.OnConfigUpdated);
                    break;
                case ConfigMethods.ProviderPrepareDefault:
                    methodName = nameof(IConfigProvider<ConfigBase>.PrepareDefaultConfig);
                    break;
                case ConfigMethods.ProviderOnSaving:
                    methodName = nameof(IConfigProvider<ConfigBase>.FetchConfigToSave);
                    break;
            }
            return methodName;
        }
    }
}
