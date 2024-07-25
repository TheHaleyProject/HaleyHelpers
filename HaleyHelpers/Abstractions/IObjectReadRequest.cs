using System;
using Haley.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;
using System.IO;

namespace Haley.Abstractions {
    public interface IObjectReadRequest {
        string ObjectLocation { get; }
        List<StorageRoute> StorageRoutes { get; }

    }
}
