using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml.Linq;
using static Haley.Internal.InternalConst;

namespace Haley.Internal {
    internal class Constants {
        public static DateTime B_EPOCH = new DateTime(2014, 11, 13); //Assuming we start couting from this date. (Not from Epoch.
    }

    internal class InternalConst {
        public const string VAULT_DEFCLIENT = "admin";
        public static string NAME = $@"@{nameof(NAME)}";
        public static string GUID = $@"@{nameof(GUID)}";
        public static string PATH = $@"@{nameof(PATH)}";
        public static string SUFFIX_DIR = $@"@{nameof(SUFFIX_DIR)}";
        public static string SUFFIX_FILE = $@"@{nameof(SUFFIX_FILE)}";
        public static string ID = $@"@{nameof(ID)}";
        public static string FULLNAME = $@"@{nameof(FULLNAME)}";
        public static string SIGNKEY = $@"@{nameof(SIGNKEY)}";
        public static string ENCRYPTKEY = $@"@{nameof(ENCRYPTKEY)}";
        public static string VALUE = $@"@{nameof(VALUE)}";
        public static string PASSWORD = $@"@{nameof(PASSWORD)}";

    }

    internal class Queries {
        public static string CLIENTPASS = $@"select password from client as c where c.id = {ID} LIMIT 1;";
        public static string CLIENTUPDATEPASS = $@"update client set password = {VALUE} where client.id = {ID};";
        public static string CLIENTEXISTS = $@"select 1 from client as c where c.name = {NAME} LIMIT 1;";
        public static string ADDCLIENT = $@"insert into client (name,guid,path,password) values ({NAME},{GUID},{PATH},{PASSWORD}) returning id;";
        public static string ADDENCRYPTION = $@"insert into client_keys (client,signing,encrypt) values ({ID},{SIGNKEY},{ENCRYPTKEY}) returning client;";
        public static string ADDCLIENTINFO = $@"insert into client_info (id,file_suffix,dir_suffix,full_name) values ({ID},{SUFFIX_FILE},{SUFFIX_DIR},{FULLNAME}) returning id;";
    }
}
