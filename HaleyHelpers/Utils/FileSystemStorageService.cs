using Haley.Abstractions;
using Haley.Enums;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;

namespace Haley.Utils {
    public class FileSystemStorageService : IStorageService {

        public FileSystemStorageService() {
        }

        public bool Delete(StorageInput input) {
            throw new NotImplementedException();
        }

        public bool Exists(StorageInput input) {
            throw new NotImplementedException();
        }

        public Stream Fetch(StorageInput input) {
            throw new NotImplementedException();
        }

        public long GetSize(StorageInput input) {
            throw new NotImplementedException();
        }

        public bool Store(StorageInput input) {
            var inpPr = input.Process();
            return true;
        }

        public bool Store(StorageInput input, out string storedPath) {
            throw new NotImplementedException();
        }
    }
}
