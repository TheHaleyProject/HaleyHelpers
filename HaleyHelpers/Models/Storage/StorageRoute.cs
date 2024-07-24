using System;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public class StorageRoute {
        public string Path { get; set; }
        public bool CreateIfNotFound { get; set; } = true;
        public StorageRoute(string path, bool create_if_not_found = true) { Path = path; CreateIfNotFound = create_if_not_found; }
    }
}
