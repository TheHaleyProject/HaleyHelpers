using ConsoleTest.Models;
using Haley.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Helpers {
    public class ConfigConsumerTwo : IConfigConsumer<ConfigOne>, IConfigConsumer<ConfigTwo> {
        public Guid UniqueId { get; set; }

        public Task<bool> OnConfigChanged(ConfigOne config) {
            return Task.FromResult(true);
        }

        public Task<bool> OnConfigChanged(ConfigTwo config) {
            return Task.FromResult(true);
        }
    }
}
