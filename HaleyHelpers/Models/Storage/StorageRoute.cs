using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public struct StorageRoute {
        public string Key { get; }
        public string Path { get; private set; }
        public void SetPath(string path) { Path = path; }
        public bool CanCreatePath { get; }
        public bool IsFile { get; set; }
        public StorageRoute(string key, string path, bool isfile, bool createIfMissing) { Key = key;  Path = path; CanCreatePath = createIfMissing; IsFile = isfile; }
    }
}
