using ConsoleTest.Helpers;
using ConsoleTest.Models;
using Haley.Abstractions;
using Haley.Services;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest {
    internal class ConfigTest {
        public IConfigService Cfg { get; set; }
        ConfigProvider _commonProvider = new ConfigProvider();
        ConfigConsumerOne _consumerOne = new ConfigConsumerOne(); //Could be one viewmodel which can register by its own
        ConfigConsumerThree _consumerThree = new ConfigConsumerThree(); //Could be one viewmodel which can register by its own
        ConfigConsumerTwo _consumerTwo = new ConfigConsumerTwo(); //Could be one viewmodel which can register by its own
        public void RegisterTest() {
            //RegisterTest all the configurations.
            Cfg.TryRegister<ConfigOne>(); //RegisterTest Config
            Cfg.TryRegister<ConfigTwo>(_commonProvider, new List<IConfigConsumer<ConfigTwo>>() { _consumerTwo}, true);
            Cfg.TryRegisterConsumer<ConfigTwo>(_consumerOne);
            Cfg.TryRegisterConsumer<ConfigOne>(_consumerThree);
            Cfg.TryRegisterOrUpdateProvider<ConfigOne>(_commonProvider); 
        }

        public async Task SaveConfigTest() {
            await Cfg.Save<ConfigTwo>(); //This will call a specific config and save it.
            await Cfg.Save<ConfigOne>(); //This will call a specific config and save it.
        }
      
        public ConfigTest() { 
            Cfg = new ConfigManagerService();
            //Cfg.SetProcessors(saveProcessor, loadProcessor);
        }

        private string loadProcessor(Type arg1, string arg2) {
            if (arg1 == typeof(ConfigOne)) {
                return arg2.Decrypt("sw234mkzsx9c1sd", "olacabs");
            }
            return arg2;
        }

        private string saveProcessor(Type arg1, string arg2) {
            //Regardless of whatever is the type, just encrypt it.
            if (arg1 == typeof(ConfigOne)) {
                var encrypted = arg2.Encrypt("sw234mkzsx9c1sd", "olacabs");
                return encrypted.value;
            }
            return arg2;
        }
    }
}
