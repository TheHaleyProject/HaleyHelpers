﻿using Haley.Enums;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Haley.Utils
{
    public static class HashUtils
    {
        #region GetRandom
        public static string GetRandomString(int number_of_bits = 1024)
        {
            return Convert.ToBase64String(GetRandomBytes(number_of_bits).bytes);
        }
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
            byte[] _salt = GetRandomBytes(salt_bits).bytes;//Getting random bytes of specified length to use as a key.
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
                throw ex;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(_pointer);
            }
        }
        public static int CalculateBytes(int number_of_bits)
        {
            var length = (int)Math.Round(number_of_bits / 8.0, MidpointRounding.AwayFromZero);
            if (length < 8) length = 8;
            return length;
        }
        #endregion

        #region ComputeHashes
        public static string ComputeHash(Stream buffered_stream, HashMethod method = HashMethod.MD5, bool encodeBase64 = true)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                buffered_stream.CopyTo(ms);
                return _convertToString(ComputeHash(ms.ToArray(), method),encodeBase64);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                ms.Dispose();
            }

        }
        public static string ComputeHash(string to_hash, HashMethod method = HashMethod.MD5, bool encodeBase64 =true)
        {
            var _to_hash_bytes = Encoding.ASCII.GetBytes(to_hash);
            return _convertToString(ComputeHash(_to_hash_bytes, method), encodeBase64);
        }
        public static string ComputeHash(FileInfo file_info, HashMethod method = HashMethod.MD5, bool encodeBase64 =true)
        {
            try
            {
                if (file_info.Exists) return null;
                using (var file_stream = new FileStream(file_info.ToString(), FileMode.Open))
                {
                    using (var buffered_stream = new BufferedStream(file_stream))
                    {
                        return ComputeHash(buffered_stream,method,encodeBase64);
                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }
        public static byte[] ComputeHash(byte[] stream_array, HashMethod method = HashMethod.MD5)
        {
            byte[] computed_hash = null;
            switch (method)
            {
                case HashMethod.MD5:
                    using (var cryptoProvider = new MD5CryptoServiceProvider())
                    {
                        computed_hash = cryptoProvider.ComputeHash(stream_array);
                    }
                    break;
                case HashMethod.Sha256:
                    using (var cryptoProvider = new SHA256CryptoServiceProvider())
                    {
                        computed_hash = cryptoProvider.ComputeHash(stream_array);
                    }
                    break;
                case HashMethod.Sha512:
                    using (var cryptoProvider = new SHA512CryptoServiceProvider())
                    {
                        computed_hash = cryptoProvider.ComputeHash(stream_array);
                    }
                    break;
                default:
                case HashMethod.Sha1:
                    using (var cryptoProvider = new SHA1CryptoServiceProvider())
                    {
                        computed_hash = cryptoProvider.ComputeHash(stream_array);
                    }
                    break;
            }
            return computed_hash;
        }
        private static string _convertToString(byte[] input, bool encodeBase64)
        {
            switch(encodeBase64)
            {
                case true:
                    return Convert.ToBase64String(input);
                default:
                case false:
                    return BitConverter.ToString(input);
            }
        }
        #endregion
    }
}
