using Haley.Enums;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Haley.Utils
{
    public static class HashUtils
    {
        #region GetRandom
        
        public static (int length, byte[] bytes) GetRandomBytes(int number_of_bits)
        {
            var length = CalculateBytes(number_of_bits);
            var _byte = new byte[length];
            using (var rng = new RNGCryptoServiceProvider()) { rng.GetNonZeroBytes(_byte); } // Generating random bytes of the provided length values.
            return (length, _byte);
        }
        #endregion

        #region HashPasswords
        public static (string salt, string value) GetHashWithSalt(SecureString to_hash, int salt_bits = 1024, int value_bits = 1024)
        {
            return GetHashWithSalt(UnWrap(to_hash), salt_bits, value_bits);
        }
        public static (string salt, string value) GetHashWithSalt(string to_hash, int salt_bits = 1024, int value_bits = 1024)
        {
            var value_length = CalculateBytes(value_bits);
            byte[] _salt = GetRandomBytes(salt_bits).bytes;//Getting random bytes of specified length to use as a Key.
            byte[] _value = null;
            using (var _rfcProcessor = new Rfc2898DeriveBytes(to_hash, _salt)) // Hashing the provided string, with the random generated or user provided bytes.
            {
                _value = _rfcProcessor.GetBytes(value_length); // Getting the bytes of the hashed value
            }
            return (Convert.ToBase64String(_salt), Convert.ToBase64String(_value));
        }
        /// <summary>
        /// Get the hash with an existing salt
        /// </summary>
        /// <param name="to_hash"></param>
        /// <param name="salt">Salt in base 64</param>
        /// <param name="value_bits"></param>
        /// <returns></returns>
        public static string GetHash(SecureString to_hash, string salt, int value_bits = 1024)
        {
            return GetHash(UnWrap(to_hash), salt, value_bits);
        }
        public static string GetHash(string to_hash, string salt, int value_bits = 1024)
        {
            if (!salt.IsBase64()) return null;
            byte[] _salt = Convert.FromBase64String(salt);
            byte[] _value = null;

            var value_length = CalculateBytes(value_bits);

            using (var _rfcProcessor = new Rfc2898DeriveBytes(to_hash, _salt)) // Hashing the provided string, with the random generated or user provided bytes.
            {
                _value = _rfcProcessor.GetBytes(value_length); // Getting the bytes of the hashed value
            }
            return Convert.ToBase64String(_value);
        }
        public static string UnWrap(SecureString input_secure_string)
        {
            if (input_secure_string == null) return null;
            IntPtr _pointer = IntPtr.Zero;
            try
            {
                _pointer = Marshal.SecureStringToGlobalAllocUnicode(input_secure_string);
                return Marshal.PtrToStringUni(_pointer);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(_pointer);
            }
        }
        public static int CalculateBytes(int number_of_bits)
        {
            var byte_length = (int)Math.Round(number_of_bits / 8.0, MidpointRounding.AwayFromZero);
            if (byte_length < 1) byte_length = 1; //We need atleast one byte of data
            return byte_length;
        }
        #endregion

        public static Guid CreateGUID(this string input,HashMethod method = HashMethod.MD5)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new ArgumentNullException("Input is null. Cannot create GUID.");
            var inputbytes = Encoding.UTF8.GetBytes(input.Trim());

            //Let us only start with either md5 or sha256.
            if (!(method == HashMethod.MD5 || method == HashMethod.Sha256)) method = HashMethod.MD5;
            var hash = ComputeHash(inputbytes, method);
            //Remember MD5 produces 128bits (16 bytes) and sha256 produces 256bits (32bytes). For a guid to work, we need only 16bytes. So, if we decide to use sha256, then we need to trim the resulting hash bytes before processing.

            //RFC4122: As per the RFC4122, all GUID should contain version related information stored in byte 6 and variant information stored in byte 8.
            //####### BITWISE OPERATIONS BELOW.############ 
            //STEP 1: CLEAR THE UPPER 4BITS AND THE THEN SET THE LOWER 4BITS
            hash[6] = (byte)((hash[6] & 0x0F) | 0X40);//0x0F is 00001111 (THE UPPER FOURBITS WILL BE REMOVED BECAUSE OF 0000 AND THE LOWER 4 BITS WILL BE REATINED.  0X40 IS NOTHING BUT 0100 0000 . With this, our resulting information in byte 6 will be in the format of 0100XXXX.

            hash[8] = (byte)((hash[8] & 0X3F) | 0x80); // 0x3F clears upper 2 bits. 0x80 SETS THE top two bits to 10. So, we get the format as 10XXXXXX

            if (method == HashMethod.MD5) return new Guid(hash);

            byte[] guidBytes = new byte[16];
            Array.Copy(hash, 0, guidBytes, 0, 16);
            return new Guid(guidBytes);
        }

        #region Hash
        public static string ComputeHash(Stream buffered_stream, HashMethod method = HashMethod.MD5, bool encodeBase64 = true) {
            MemoryStream ms = new MemoryStream();
            try {
                buffered_stream.CopyTo(ms);
                return ConvertToString(ComputeHash(ms.ToArray(), method), encodeBase64);
            } catch (Exception) {
                throw;
            } finally {
                ms.Dispose();
            }

        }
        public static string ComputeHash(this string to_hash, HashMethod method = HashMethod.MD5, bool encodeBase64 = true) {
            return ConvertToString(ComputeHashBytes(to_hash,method), encodeBase64);
        }
        public static byte[] ComputeHashBytes(this string to_hash, HashMethod method = HashMethod.MD5) {
            var _to_hash_bytes = Encoding.UTF8.GetBytes(to_hash);
            return ComputeHash(_to_hash_bytes, method);
        }
        public static string ComputeHash(FileInfo file_info, HashMethod method = HashMethod.MD5, bool encodeBase64 = true) {
            try {
                if (file_info.Exists) return null;
                using (var file_stream = new FileStream(file_info.ToString(), FileMode.Open)) {
                    using (var buffered_stream = new BufferedStream(file_stream)) {
                        return ComputeHash(buffered_stream, method, encodeBase64);
                    }
                }

            } catch (Exception) {
                throw;
            }
        }
        public static byte[] ComputeHash(byte[] stream_array, HashMethod method = HashMethod.MD5) {
            byte[] computed_hash = null;
            switch (method) {
                case HashMethod.MD5:
                //NOTE: HMACMD5 generates different hash for same input.
                //HMACMD5 is used for sending a code not for Hash.
                using (var cryptoProvider = new MD5CryptoServiceProvider()) {
                    computed_hash = cryptoProvider.ComputeHash(stream_array);
                }
                break;
                case HashMethod.Sha256:
                using (var cryptoProvider = new SHA256Managed()) {
                    computed_hash = cryptoProvider.ComputeHash(stream_array);
                }
                break;
                case HashMethod.Sha512:
                using (var cryptoProvider = new SHA512Managed()) {
                    computed_hash = cryptoProvider.ComputeHash(stream_array);
                }
                break;
                default:
                case HashMethod.Sha1:
                using (var cryptoProvider = new SHA1Managed()) {
                    computed_hash = cryptoProvider.ComputeHash(stream_array);
                }
                break;
            }
            return computed_hash;
        }
        #endregion

        #region Signature
        public static string ComputeSignature(this Stream buffered_stream, string key,  HashMethod method = HashMethod.MD5, bool encodeBase64 = true) {
            MemoryStream ms = new MemoryStream();
            try {
                buffered_stream.CopyTo(ms);
                return ConvertToString(ComputeSignature(ms.ToArray(), key, method), encodeBase64);
            } catch (Exception) {
                throw;
            } finally {
                ms.Dispose();
            }

        }
        public static string ComputeSignature(this string to_hash, string key, HashMethod method = HashMethod.MD5, bool encodeBase64 = true) {
            var hashBytes = Encoding.UTF8.GetBytes(to_hash);
            return ConvertToString(ComputeSignature(hashBytes,key, method), encodeBase64);
        }
        public static string ComputeSignature(this FileInfo file_info, string key, HashMethod method = HashMethod.MD5, bool encodeBase64 = true) {
            try {
                if (file_info.Exists) return null;
                using (var file_stream = new FileStream(file_info.ToString(), FileMode.Open)) {
                    using (var buffered_stream = new BufferedStream(file_stream)) {
                        return ComputeSignature(buffered_stream, key,method, encodeBase64);
                    }
                }

            } catch (Exception) {
                throw;
            }
        }
        public static byte[] ComputeSignature(this byte[] stream_array, string key,  HashMethod method = HashMethod.MD5) {
            byte[] computed_hash = null;
            byte[] keyBytes = key.GetBytes();
            switch (method) {
                case HashMethod.MD5:
                //NOTE: HMACMD5 generates different hash for same input.
                //HMACMD5 is used for sending a code not for Hash.
                using (var cryptoProvider = new HMACMD5(keyBytes)) {
                    computed_hash = cryptoProvider.ComputeHash(stream_array);
                }
                break;
                case HashMethod.Sha256:
                using (var cryptoProvider = new HMACSHA256(keyBytes)) {
                    computed_hash = cryptoProvider.ComputeHash(stream_array);
                }
                break;
                case HashMethod.Sha512:
                using (var cryptoProvider = new HMACSHA512(keyBytes)) {
                    computed_hash = cryptoProvider.ComputeHash(stream_array);
                }
                break;
                default:
                case HashMethod.Sha1:
                using (var cryptoProvider = new HMACSHA1(keyBytes)) {
                    computed_hash = cryptoProvider.ComputeHash(stream_array);
                }
                break;
            }
            return computed_hash;
        }
        #endregion
        private static string ConvertToString(byte[] input, bool encodeBase64)
        {
            StringBuilder hashresult = new StringBuilder();

            foreach (var byt in input) {
                hashresult.Append(byt.ToString("x2"));
            }

            string result = hashresult.ToString();

            if (encodeBase64) result = Convert.ToBase64String(Encoding.UTF8.GetBytes(result));

            return result;
        }
    }
}
