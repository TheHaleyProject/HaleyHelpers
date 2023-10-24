using ConsoleTest.Models;
using Haley.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Helpers {
    internal class ConfigConsumerOne : IConfigConsumer<ConfigTwo> {
        public Guid UniqueId { get; set; }

        public Task<bool> OnConfigUpdated(ConfigTwo config) {
            return Task.FromResult(true);
        }
    }
}
