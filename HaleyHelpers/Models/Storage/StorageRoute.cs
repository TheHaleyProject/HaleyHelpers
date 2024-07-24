using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public class StorageRoute {
        public string Key { get; }
        public string Path { get; private set; }

        public void SetPath(string path) { Path = path; }
        public bool CreateIfMissing { get; } = true;
        public StorageRoute(string key, string path, bool createIfMissing = true) { Key = key;  Path = path; CreateIfMissing = createIfMissing; }
    }
}
