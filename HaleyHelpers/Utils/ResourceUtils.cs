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
    public static class ResourceUtils
    {
        public static string[] GetResourceNames(Assembly assembly_name = null) {
            if (assembly_name == null) {
                assembly_name = Assembly.GetCallingAssembly();
            }
            return assembly_name.GetManifestResourceNames();
        }

        public static bool DownloadEmbeddedResource(string resource_name, string save_dir_path, string save_file_name, Assembly assembly_name = null)
        {
            try
            {
                if (assembly_name == null) assembly_name = Assembly.GetCallingAssembly();
                if (save_file_name == null) save_file_name = resource_name; //use same resource name as target name
                string full_file_path = Path.Combine(save_dir_path, save_file_name);
                return DownloadEmbeddedResource(resource_name, full_file_path, assembly_name);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static bool DownloadEmbeddedResource(string resource_name, string save_file_path, Assembly assembly_name=null)
        {
            try
            {
                if (assembly_name == null) assembly_name = Assembly.GetCallingAssembly();
                var _streambyte = GetEmbeddedResource(resource_name, assembly_name);
                File.WriteAllBytes(save_file_path, _streambyte);
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static byte[] GetEmbeddedResource(string full_resource_name, Assembly assembly_name)
        {
            try
            {
                var _stream = assembly_name.GetManifestResourceStream(full_resource_name); //Get the resource from the assembly
                if (_stream == null) return null;

                byte[] _stream_byte = new byte[_stream.Length]; //initiate a byte array
                using (var memstream = new MemoryStream())
                {
                    _stream.CopyTo(memstream);
                    _stream_byte = memstream.ToArray();
                }

                return _stream_byte;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
