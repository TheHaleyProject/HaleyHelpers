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
        Task<StorageOutput> Store(StorageInput input, Stream file);
        Stream Fetch(StorageInput input);
        bool Delete(StorageInput input);
        bool Exists(StorageInput input);
        long GetSize(StorageInput input);
    }
}
