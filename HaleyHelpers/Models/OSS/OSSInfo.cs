using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public class OSSInfo  {
        public const string DEFAULTNAME = "default";
        public string Name { get; private set; }
        private string _displayName;

        public string DisplayName {
            get { return _displayName; }
            set {
                _displayName = value ?? DEFAULTNAME;
                if (!string.IsNullOrWhiteSpace(_displayName)) {
                    Name = _displayName.Trim().ToLower().Replace(" ", "_");
                }
            }
        }
        public string Guid { get; set; } //Name with which it is identified
    }
}
