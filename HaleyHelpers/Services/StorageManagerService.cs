using Haley.Abstractions;
using Haley.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Haley.Helpers.Services {
    public class StorageManagerService : ConcurrentDictionary<string, IStorageService>, IStorageManager {
       
    }
}
