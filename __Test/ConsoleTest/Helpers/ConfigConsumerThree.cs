using ConsoleTest.Models;
using Haley.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Helpers {
    internal class ConfigConsumerThree : IConfigConsumer<ConfigOne> {
        public Guid UniqueId { get; set; }

        public async Task<bool> OnConfigChanged(ConfigOne config) {
            return true;
        }
    }
}
