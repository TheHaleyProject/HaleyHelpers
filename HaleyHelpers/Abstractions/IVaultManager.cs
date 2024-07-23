using System;
using Haley.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;
using System.IO;
using System.Collections.Concurrent;

namespace Haley.Abstractions {
    public interface IVaultManager : IDictionary<string,IObjectStorageService> {
        //We need Store, Fetch, Delete
      
    }
}
