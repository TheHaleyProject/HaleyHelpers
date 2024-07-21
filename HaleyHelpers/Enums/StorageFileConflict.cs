using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haley.Enums {
    public enum StorageFileConflict {
        Skip = 0,
        ReturnError=1,
        Replace = 2,
        //ThrowException=3

    }
}
