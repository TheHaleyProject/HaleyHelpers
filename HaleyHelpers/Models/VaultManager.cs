using Haley.Abstractions;
using System.Collections.Concurrent;

namespace Haley.Models {
    public class VaultManager : ConcurrentDictionary<string, IObjectStorageService>, IVaultManager {
       
    }
}
