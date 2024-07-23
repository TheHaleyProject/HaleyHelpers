using Haley.Abstractions;
using Haley.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Haley.Utils {
    public class VaultManager : ConcurrentDictionary<string, IObjectStorageService>, IVaultManager {
       
    }
}
