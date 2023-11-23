
using ConsoleTest.Models;
using Haley.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Helpers {

    // null is the default value of reference-type variables. So, new ConfigOne() will be null.
    public class ConfigProvider : IConfigProvider<ConfigOne>, IConfigProvider<ConfigTwo> {
        //public class ConfigProvider : IConfigProvider<ConfigOne> {
        public Guid UniqueId { get; set; }
        public async Task<ConfigOne> GetLatestConfig() {
            var cfg = new ConfigOne();
            cfg.Price = 6599;
            //return Task.FromResult(cfg);
            await Task.Delay(1000);
            return cfg;
        }

        public Task<ConfigOne> PrepareDefaultConfig() {
            return Task.FromResult(new ConfigOne() { Id = "Default"});
        }

        async Task<ConfigTwo> IConfigProvider<ConfigTwo>.PrepareDefaultConfig() {
            return null;
        }


        Task<ConfigTwo> IConfigProvider<ConfigTwo>.GetLatestConfig() {
            return Task.FromResult(new ConfigTwo() { Message = "Ola.. Welcome.." });
        }

        public async Task EmptyTest() {
            string msg = "nothing";
            return;
        }

        public void DummyMethod() {
            //do nothing.
        }
    }
}
