using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public class OSSModule : OSSDirectory {
        public OSSInfo Client { get; set; }
        public string DatabaseName { get; set; }
        public void Assert() {
            if (string.IsNullOrEmpty(SaveAsName) || string.IsNullOrEmpty(DisplayName) || string.IsNullOrEmpty(Path)) throw new ArgumentNullException("Name & Path Cannot be empty");
            if (Client == null || string.IsNullOrEmpty(Client.Name)) throw new ArgumentNullException("Client Name cannot be empty");
        }
        public OSSModule(string clientName) {
            Client = new OSSInfo() { DisplayName = clientName };
        }
        public OSSModule() { }
    }
}
