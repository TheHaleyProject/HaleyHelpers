using System;
using Haley.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;
using System.IO;

namespace Haley.Abstractions {
    public interface IStorageService {
        //We need Store, Fetch, Delete
        bool Store(StorageInput input,out string storedPath);
        bool Store(StorageInput input);
        Stream Fetch(StorageInput input);
        bool Delete(StorageInput input);
        bool Exists(StorageInput input);
        long GetSize(StorageInput input);
    }
}
