using ConsoleTest.Helpers;
using ConsoleTest.Models;
using Haley.Abstractions;
using Haley.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest {
    internal class ConfigTest {
        IConfigService _cfg;
        ConfigProvider _commonProvider = new ConfigProvider();
        ConfigConsumerOne _consumerOne = new ConfigConsumerOne(); //Could be one viewmodel which can register by its own
        ConfigConsumerThree _consumerThree = new ConfigConsumerThree(); //Could be one viewmodel which can register by its own
        ConfigConsumerTwo _consumerTwo = new ConfigConsumerTwo(); //Could be one viewmodel which can register by its own
        public void Register() {
            //Register all the configurations.
            _cfg.TryRegister<ConfigOne>(); //Register Config
            _cfg.TryRegister<ConfigTwo>(_commonProvider, new List<IConfigConsumer<ConfigTwo>>() { _consumerTwo}, true);
            _cfg.TryRegisterConsumer<ConfigTwo>(_consumerOne);
            _cfg.TryRegisterConsumer<ConfigOne>(_consumerThree);
            _cfg.TryRegisterOrUpdateProvider<ConfigOne>(_commonProvider); 
        }

        public void SaveConfig<T>() where T : class,IConfig,new() {
            _cfg.Save<T>(); //This will call a specific config and save it.
        }

        public T GetConfig<T>() where T : class, IConfig, new() {
            return _cfg.GetConfig<T>();
        }

        public void LoadConfig<T>() where T : class,IConfig,new() {
            _cfg.LoadConfig<T>();
        }

        public ConfigTest() { _cfg = new ConfigManagerService(); }
    }
}
