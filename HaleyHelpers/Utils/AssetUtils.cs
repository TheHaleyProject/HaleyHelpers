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

namespace Haley.Utils
{
    public static class AssetUtils
    {
        const string QRY_MO_SNU = @"SELECT SerialNumber FROM Win32_BaseBoard";
        const string QRY_PR_PID = @"SELECT ProcessorId FROM Win32_processor";
        const string QRY_CS_UNA = @"SELECT UserName FROM Win32_ComputerSystem";

        static string GetPropertyName(IDObject target) {
            switch (target) {
                case IDObject.MotherBoardID:
                return "SerialNumber";
                case IDObject.ProcessorID:
                return "ProcessorId";
                case IDObject.ComputerUserName:
                return "UserName";
            }
            return "Name";
        }

        static string GetQuery(IDObject target) {
            switch (target) {
                case IDObject.MotherBoardID:
                return QRY_MO_SNU;
                case IDObject.ProcessorID:
                return QRY_PR_PID;
                case IDObject.ComputerUserName:
                return QRY_CS_UNA;
            }
            return string.Empty;
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

        public static string GetID(IDObject target)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return string.Empty;
                var qry = GetQuery(target);
                if (string.IsNullOrWhiteSpace(qry)) return null;
                var mo_searcher = new ManagementObjectSearcher(qry);
                string result = string.Empty;
                foreach (var mo in mo_searcher.Get()) {
                    var pname = GetPropertyName(target);
                    result = mo[pname].ToString();
                    break;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static string GetMachineId()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return string.Empty;
                return (GetID(IDObject.MotherBoardID) + "###" +GetID(IDObject.ProcessorID));
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
