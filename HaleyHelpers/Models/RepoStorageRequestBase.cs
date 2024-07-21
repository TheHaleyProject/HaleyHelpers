using Haley.Enums;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Haley.Models {
    public class RepoStorageRequestBase {
        public StorageRequestBase RepoInfo { get; set; } = new StorageRequestBase();
        public string Path { get; set; } //Could be number or string | This is supposedly the parameter key which user provides while making the request.
        public string Name { get; set; }
        public RepoStorageRequestBase() {  }
    }
}
