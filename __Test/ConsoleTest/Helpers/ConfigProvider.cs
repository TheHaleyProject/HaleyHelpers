
using ConsoleTest.Models;
using Haley.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Helpers {

   // null is the default value of reference-type variables. So, new ConfigOne() will be null.
    public class ConfigProvider : IConfigProvider<ConfigOne>, IConfigProvider<ConfigTwo> {
        public Guid UniqueId { get; set; }
        public Task<ConfigOne> FetchConfigToSave() {
            return Task.FromResult(new ConfigOne());
        }

        public Task<ConfigOne> PrepareDefaultConfig() {
            return Task.FromResult(new ConfigOne());
        }

        Task<ConfigOne> IConfigProvider<ConfigOne>.PrepareDefaultConfig(bool test) {
            return Task.FromResult(new ConfigOne());
        }

        public Task<ConfigTwo> PrepareDefaultConfig(bool test = false) {
            return Task.FromResult(new ConfigTwo());
        }
        Task<ConfigTwo> IConfigProvider<ConfigTwo>.PrepareDefaultConfig() {
            return Task.FromResult(new ConfigTwo());
        }


        Task<ConfigTwo> IConfigProvider<ConfigTwo>.FetchConfigToSave() {
            return Task.FromResult(new ConfigTwo());
        }
    }
}
