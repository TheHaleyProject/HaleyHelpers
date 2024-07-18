using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haley.Enums {
    public enum StorageNameHashMode {
       ParseOrCreate = 0, //Try to parse, if not found, then create hash.
       Parse=1, //Will try to parse, if it is not found, sends error.
       Force =2 //Regardless if it's already present, we hash once more.
    }
}
