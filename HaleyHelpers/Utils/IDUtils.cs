using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.IO;
using System.Reflection;

namespace Haley.Utils
{
    public static class IDUtils
    {
        public static string GetMotherBoardID()
        {
            try
            {
                //SelectQuery query = new SelectQuery("Win32_processor");
                //var mng_obj_searcher = new ManagementObjectSearcher(query);
                var mng_obj_searcher = new ManagementObjectSearcher("Select SerialNumber From Win32_BaseBoard");
                var collection = mng_obj_searcher.Get();
                string id = null;
                    foreach (ManagementObject mgt_obj in collection)
                    {
                        id = mgt_obj["SerialNumber"].ToString();
                        break;
                    }
                return id;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        public static string GetProcessorID()
        {
            try
            {
                var mng_obj_searcher = new ManagementObjectSearcher("Select * From Win32_processor");
                var collection = mng_obj_searcher.Get();
                string id = null;
                    foreach (ManagementObject mgt_obj in collection)
                    {
                        id = mgt_obj["ProcessorId"].ToString();
                        break;
                    }
                return id;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static string GetID()
        {
            try
            {
                return (GetMotherBoardID() + "###" +GetProcessorID());
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
