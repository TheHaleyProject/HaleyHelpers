using Haley.Abstractions;
using Haley.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public class OSSModule : OSSDirectory, IOSSModule {
        public IOSSInfo Client { get; set; }
        public string DatabaseName { get; set; }
        public override bool TryValidate(out string message) {
            message = string.Empty;
            if (!base.TryValidate(out message)) return false;
            if (string.IsNullOrEmpty(SaveAsName) || string.IsNullOrEmpty(Path)) {
                message = "Name & Path Cannot be empty";
                return false;
            }
            if (Client == null || string.IsNullOrEmpty(Client.Name)) {
                message = "Client Information cannot be empty";
                return false;
            }
            return true;
        }
        public OSSModule(string clientName, string displayName = DEFAULTNAME) : base(displayName) {
            Client = new OSSInfo(clientName) {};
            GenerateCuid(Client.Name, Name); //With all other names
        }
    }
}
