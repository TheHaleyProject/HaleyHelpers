using Haley.Abstractions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Haley.Models {
    public class VaultManager : ConcurrentDictionary<string, IObjectStorageService>, IVaultManager {
       
    }
}
