using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.IO;
using System.Reflection;
using Haley.Enums;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Haley.Models;

//List<(string query, string scope)> queryTuple = new List<(string query, string scope)>();
//queryTuple.Add(("select * from win32_bios", string.Empty));
//queryTuple.Add(("select * from win32_baseboard", string.Empty));
////queryTuple.Add(("select * from Win32_LogicalDisk", string.Empty));
////queryTuple.Add(("select * from Win32_physicalmedia", string.Empty));
////queryTuple.Add(("select * from Win32_diskdrive", string.Empty));
////queryTuple.Add(("select * from Win32_PhysicalMemory", string.Empty));
////queryTuple.Add(("select * from Win32_desktopmonitor", string.Empty));
////queryTuple.Add(("select * from Win32_ComputerSystem", string.Empty));
////queryTuple.Add(("select * from Win32_operatingsystem", string.Empty));
//queryTuple.Add(("select * from Win32_Keyboard", "root\\cimv2"));
//queryTuple.Add(("select * from Win32_SoundDevice", "root\\cimv2"));
//queryTuple.Add(("select * from Win32_LogonSession", "root\\cimv2"));
//queryTuple.Add(("select * from Win32_PointingDevice", "root\\cimv2"));
//queryTuple.Add(("select * from WmiMonitorID", "root\\wmi"));
////queryTuple.Add(("select * from WmiMonitorBasicParams", "root\\wmi"));


////Get - WmiObject - Class Win32_BIOS
////    Get-WmiObject -Class Win32_ComputerSystem
////    Get-WmiObject -Class Win32_OperatingSystem
////    Get-WmiObject -Class Win32_NetworkAdapter
////    Get-WmiObject -Class Win32_NetworkAdapterConfiguration
////    Get-WmiObject -Class Win32_Product

namespace Haley.Utils
{
    public static class AssetUtils
    {
        const string QRY_MO_SNU = @"SELECT SerialNumber FROM Win32_BaseBoard";
        const string QRY_PR_PID = @"SELECT ProcessorId FROM Win32_processor";
        const string QRY_CS_UNA = @"SELECT UserName FROM Win32_ComputerSystem";

        static (AssetType type,string prop) GetAssetInfo (AssetIdentifier target) {
            switch (target) {
                case AssetIdentifier.MotherBoardID:
                return (AssetType.MotherBoard, "SerialNumber");
                case AssetIdentifier.ProcessorID:
                return (AssetType.Processor, "ProcessorId");
                case AssetIdentifier.ComputerUserName:
                return (AssetType.Computer, "UserName");
                case AssetIdentifier.BIOSID:
                return (AssetType.BIOS, "SerialNumber");
            }
            throw new NotImplementedException();
        }


        public static string GetUserSID(string userName) {
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return string.Empty;
                NTAccount ntAccount = new NTAccount(userName);
                SecurityIdentifier sid = (SecurityIdentifier)ntAccount.Translate(typeof(SecurityIdentifier));
                return sid.Value;
            } catch (Exception ex) {
                throw;
            }
        }

        public static List<Dictionary<string, object>> GetProperties(AssetType target, string[] propNames = null) {
            return GetPropertiesInternal(target, propNames);
        }

        static List<Dictionary<string, object>> GetPropertiesInternal(AssetType target, string[] filter = null) {
            try {
                List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return result;
                var qry = $@"select * from {target.GetDescription()}";
                var mo_searcher = new ManagementObjectSearcher(qry);
                var scope = target.GetAttributeValue<ScopeAttribute>();
                if (!string.IsNullOrWhiteSpace(scope)) mo_searcher.Scope = new ManagementScope(scope);
                foreach (var mo in mo_searcher.Get()) {
                    Dictionary<string, object> localRes = new Dictionary<string, object>();
                    if (filter != null && filter.Length > 0) {
                        foreach (var prop in filter.Distinct()) {
                            try {
                                localRes.Add(prop, mo[prop]);
                            } catch (Exception) {
                                continue;
                            }
                        }
                    } else {
                        foreach (var moProp in mo.Properties) {
                            try {
                                localRes.Add(moProp.Name, moProp.Value);
                            } catch (Exception) {
                                continue;
                            }
                        }
                    }
                    result.Add(localRes);
                }
                return result;
            } catch (Exception) {
                throw;
            }
        }

        public static string GetId(AssetIdentifier target)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return string.Empty;
                var info = GetAssetInfo(target);
                var propResult = GetPropertiesInternal(info.type, new string[] { info.prop });
                return propResult.First()?.Values?.First()?.ToString() ?? null;
            } catch (Exception) {
                throw;
            }
        }

        public static string GetMachineId()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return string.Empty;
                return (GetId(AssetIdentifier.MotherBoardID) + "###" +GetId(AssetIdentifier.ProcessorID));
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
