using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using CryptXML = System.Security.Cryptography.Xml;
using System.IO;
using System.Xml;
using Haley.Enums;
using Haley.Models;

namespace Haley.Internal
{
    internal sealed class EncryptionHelper
    {
        internal sealed class AES
        {
            public static byte[] Execute(byte[] to_execute, byte[] key, byte[] salt, bool is_encrypt)
            {
                try
                {
                    using (RijndaelManaged rjManaged = new RijndaelManaged())
                    {
                        rjManaged.Padding = PaddingMode.PKCS7;
                        rjManaged.Mode = CipherMode.CBC;

                        var combinedkey = new Rfc2898DeriveBytes(key, salt, 100);
                        var _new_key = combinedkey.GetBytes(rjManaged.KeySize / 8);
                        var _new_salt = combinedkey.GetBytes(rjManaged.BlockSize / 8);

                        using (MemoryStream mstream = new MemoryStream())
                        {
                            ICryptoTransform cryptor = null;

                            //Based on the Method, we will either create a encryptor or decryptor using the Key and iv
                            switch (is_encrypt)
                            {
                                case true: //Then write the stream using an encryptor
                                    cryptor = rjManaged.CreateEncryptor(_new_key, _new_salt); //Encryptor
                                    break;

                                case false: //Then write the stream using a decryptor.
                                    cryptor = rjManaged.CreateDecryptor(_new_key, _new_salt); //Decryptor
                                    break;
                            }

                            using (CryptoStream cstream = new CryptoStream(mstream, cryptor, CryptoStreamMode.Write))
                            {
                                cstream.Write(to_execute, 0, to_execute.Length);
                                cstream.FlushFinalBlock(); //We are using specified length of Key and salt to encrypt. So the last block might not be perfect. It can be empty. So, we are flushing it.
                            }

                            //Instead of above Method, we can also use a Method where we load the memory stream with the byte array of the encrypted text. Then the cryptostream will be reading the memory stream and return the results.
                            var result = mstream.ToArray();
                            return result;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
        internal sealed class RSA
        {
            public static (string public_key, string private_key) GetXMLKeyPair()
            {
                try
                {
                    var rsa_provider = new RSACryptoServiceProvider(1024);
                    return (rsa_provider.ToXmlString(false), rsa_provider.ToXmlString(true));
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            public static byte[] Execute(byte[] to_execute, string _key, bool is_encrypt)
            {
                //IMPORTANT: PUBLIC KEY IS FOR ENCRYPTION AND PRIVATE KEY IS FOR DECRYPTION.
                var rsa_provider = new RSACryptoServiceProvider();
                try
                {
                    rsa_provider.FromXmlString(_key);
                    var padding = RSAEncryptionPadding.OaepSHA1;
                    switch (is_encrypt)
                    {
                        case true:
                            return rsa_provider.Encrypt(to_execute, padding);
                        case false:
                            return rsa_provider.Decrypt(to_execute, padding);
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    rsa_provider.Dispose();
                }
            }
        }
        internal sealed class XML
        {
            public static void Sign(XmlDocument input_doc, out XmlDocument output_doc, string private_key)
            {
                try
                {
                    //Setup RSA provider using the provided private Key
                    var rsa_provider = new RSACryptoServiceProvider();
                    rsa_provider.FromXmlString(private_key);

                    //Create a temporary xml based on the input XML. Add the Signing Key created in previous step.
                    CryptXML.SignedXml _temporary_xml = new CryptXML.SignedXml(input_doc);
                    _temporary_xml.SigningKey = rsa_provider;

                    //Reference is indicating what to Sign inside the XML. In our case, we need the whole xml document to Sign. So set the URI as ""
                    CryptXML.Reference _signing_reference = new CryptXML.Reference();
                    _signing_reference.Uri = "";

                    //Create a verification object that can be stored inside the XMl, so that it can be verified later using the public Key. Very vital step or else the verification will not be done and whole purpose of signing is defied.
                    var _verification_transform = new CryptXML.XmlDsigEnvelopedSignatureTransform();

                    //Set the verification object inside the reference object.
                    _signing_reference.AddTransform(_verification_transform);

                    //Add this reference to the XML
                    _temporary_xml.AddReference(_signing_reference);

                    //Compute
                    _temporary_xml.ComputeSignature();

                    //So far all steps are done in a temporary holder. Get the signature from there.
                    XmlElement _signature_element = _temporary_xml.GetXml();

                    output_doc = input_doc;
                    //Finally add this element to the input xml.
                    output_doc.DocumentElement.AppendChild(_signature_element);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            public static void Verify(XmlDocument input_doc, string public_key, out bool _status)
            {
                try
                {
                    _status = false;

                    //Create RSA Provider from Public Key
                    var rsa_provider = new RSACryptoServiceProvider(1024);
                    rsa_provider.FromXmlString(public_key);

                    //Create temporary xml reference using the input xml document
                    var temp_xml = new CryptXML.SignedXml(input_doc);

                    //Get the signature node for verification
                    var node_list = input_doc.GetElementsByTagName("Signature");

                    //Check if only one signature is present. If so, add it to the temporaryxml
                    if (node_list.Count != 1) return;
                    XmlElement signature_element = node_list[0] as XmlElement;
                    if (signature_element == null) return;
                    temp_xml.LoadXml(signature_element);

                    //Validate the signature
                    _status = temp_xml.CheckSignature(rsa_provider);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
        internal sealed class K2017SE
        {
            #region ATTRIBUTES
            internal const int _min_ascii_limit = 32;
            internal const int _max_ascii_limit = 126;
            #endregion

            #region Private Methods
            private static int add(int min_value, int max_value, int current_value, int to_add)
            {
                try
                {
                    int result;
                    result = current_value + to_add; // It can be within limit or more than limit
                    while (result > max_value) //Loop until the result is within max value
                    {
                        result = (min_value - 1) + (result - max_value);
                    }
                    return result;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            private static int Subtract(int min_value, int max_value, int current_value, int to_subtract)
            {
                try
                {
                    int result;
                    result = current_value - to_subtract; // It can be within limit or more than limit

                    while (result < min_value)
                    {
                        result = (max_value + 1) - (min_value - result);
                    }
                    return result;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            private static int LoopValues(int min_value, int max_value, int current_value, int modifier_value, bool isreverse)
            {
                try
                {
                    if (isreverse)
                    {
                        return Subtract(min_value, max_value, current_value, modifier_value);
                    }
                    else
                    {
                        return add(min_value, max_value, current_value, modifier_value);
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
            private static byte[] SwapForward(byte[] input_array, byte[] key_array)
            {
                try
                {
                    int key_position = 0; //Key position start

                    //Start input and Key position from zero. Sometimes, either of them will run out. For instance, if Key position is less than input position, then we need to loop Key position again.
                    for (int input_position = 0; input_position < input_array.Length; input_position++) //Loop through input array
                    {
                        var key_value_to_add = int.Parse(Encoding.ASCII.GetString(key_array, key_position, 1)); //If we get just the byte value, it will be different. We need the ascii value.
                        var position_for_swapping = LoopValues(0, input_array.Length - 1, input_position, key_value_to_add, false); //Because this is forward swap

                        //Swapping
                        var temp_value = input_array[position_for_swapping];
                        input_array[position_for_swapping] = input_array[input_position];
                        input_array[input_position] = temp_value;

                        //LOOP THROUGH KEYS AND RESET IT.
                        if (key_position == key_array.Length - 1) //Key position has reached the end
                        {
                            key_position = 0; //Reset
                        }
                        else
                        {
                            key_position++; //Increment
                        }
                    }
                    return input_array;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            private static byte[] SwapReverse(byte[] input_array, byte[] key_array) //Reversing is reversing both Key positions and also input positions
            {
                try
                {
                    int key_position = 0; //Key position start
                    //Reverse swapping is more complicated. Need to be careful while getting the current Key position.

                    if (input_array.Length <= key_array.Length) //Meaning Key length is more.
                    {
                        key_position = input_array.Length - 1;
                    }
                    else // Key length is less than the input array. We need to find the remainder
                    {
                        var remainder = (input_array.Length % key_array.Length);
                        if (remainder == 0)
                        {
                            key_position = key_array.Length - 1;
                        }
                        else
                        {
                            key_position = remainder - 1; // This gives the remainder value and reduce 1 since we are using array
                        }

                    }

                    //Input position should be from reverse
                    for (int input_position = 0; input_position < input_array.Length; input_position++) //Loop through input array in reverse order until the input position becomes zero
                    {
                        //now modify the input position , since this is reverse
                        var adjusted_input_position = (input_array.Length - 1) - input_position;

                        var key_value_to_add = int.Parse(Encoding.ASCII.GetString(key_array, key_position, 1));
                        var position_for_swapping = LoopValues(0, input_array.Length - 1, adjusted_input_position, key_value_to_add, false); //Irrespective of whether forward swap or reverse swap, we will always have the counting in forward direction only because we are not dealing with values. We are dealing with position.

                        //Swapping
                        var temp_value = input_array[position_for_swapping];
                        input_array[position_for_swapping] = input_array[adjusted_input_position];
                        input_array[adjusted_input_position] = temp_value;

                        //LOOP THROUGH KEYS AND RESET IT.
                        if (key_position == 0)
                        {
                            key_position = key_array.Length - 1; //Reset
                        }
                        else
                        {
                            key_position--; //Increment
                        }
                    }

                    return input_array;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            private static string Rotate(string input_text, long key, bool isreverse)
            {
                try
                {
                    if (string.IsNullOrEmpty(input_text)) return null;
                    //Pre Process
                    byte[] input_byte_array = Encoding.ASCII.GetBytes(input_text); //Get the bytes value of the input string
                    var key_array = key.ToString().ToArray();
                    int key_position = 0;

                    List<byte> modified_list = new List<byte>();

                    foreach (var _input_byte in input_byte_array)
                    {
                        int current_position = (int)_input_byte;
                        int new_position = LoopValues(_min_ascii_limit, _max_ascii_limit, current_position, key_array[key_position], isreverse);
                        modified_list.Add((byte)new_position);

                        //LOOP THROUGH KEYS AND RESET IT.
                        if (key_position == key_array.Length - 1)
                        {
                            key_position = 0; //Reset
                        }
                        else
                        {
                            key_position++; //Increment
                        }
                    }

                    return Encoding.ASCII.GetString(modified_list.ToArray());
                }
                catch (Exception)
                {
                    throw;
                }
            }
            private static string Swap(string input_text, long key, bool isreverse)
            {
                try
                {
                    var input_array = Encoding.ASCII.GetBytes(input_text); //Get text bytes
                    var key_array = Encoding.ASCII.GetBytes(key.ToString()); //Get Key bytes

                    string result = null;
                    switch (isreverse)
                    {
                        case true:
                            result = Encoding.ASCII.GetString(SwapReverse(input_array, key_array));
                            break;
                        case false:
                            result = Encoding.ASCII.GetString(SwapForward(input_array, key_array));
                            break;
                    }

                    return result;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            #endregion
            public static string Execute(string input_text, K2Sequence sequence)
            {

                switch (sequence.Method)
                {
                    case K2Mode.Rotate:
                        return Rotate(input_text, sequence.Key, sequence.IsReverse);
                    case K2Mode.Swap:
                        return Swap(input_text, sequence.Key, sequence.IsReverse);
                }
                return null;
            }
        }
    }
}

