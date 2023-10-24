using Haley.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Models {
    public class ConfigTwo : IConfig {
        public string Id { get; set; }

        public string FileName { get; set; }
        public string Message { get; set; }
    }
}
