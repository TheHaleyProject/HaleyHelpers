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
        Task<FileStorageSummary> Upload(StorageRequest input, Stream file, int bufferSize = 8192);
        Stream Download(StorageRequestBase input);
        bool Delete(StorageRequestBase input);
        bool Exists(StorageRequest input);
        long GetSize(StorageRequestBase input);
    }
}
