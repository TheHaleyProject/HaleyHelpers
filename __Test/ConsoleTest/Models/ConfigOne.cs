using Haley.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Models {
    public class ConfigOne : IConfig {
        public string Id { get; set; }

        public string FileName { get; set; }
        public double Price { get; set; }
        public ConfigOne() { }
    }
}
