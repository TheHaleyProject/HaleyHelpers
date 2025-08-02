using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml.Linq;
using static Haley.Internal.IndexingConstant;

namespace Haley.Internal {
    internal class Constants {
        public static DateTime B_EPOCH = new DateTime(2014, 11, 13); //Assuming we start couting from this date. (Not from Epoch.
    }

    internal class IndexingConstant {
        public const string VAULT_DEFCLIENT = "admin";
        public static string NAME = $@"@{nameof(NAME)}";
        public static string DNAME = $@"@{nameof(DNAME)}";
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
        public static string DATETIME = $@"@{nameof(DATETIME)}";
        public static string PARENT = $@"@{nameof(PARENT)}";

    }

    internal class IndexingQueries {
        public class CLIENT {
            public static string EXISTS = $@"select c.id from client as c where c.name = {NAME} LIMIT 1;";
            public static string UPSERTKEYS = $@"insert into client_keys (client,signing,encrypt,password) values ({ID},{SIGNKEY},{ENCRYPTKEY},{PASSWORD}) ON DUPLICATE KEY UPDATE signing =  VALUES(signing), encrypt = VALUES(encrypt), password = VALUES(password);";
            public static string UPSERT = $@"insert into client (name,display_name, hash_guid,path) values ({NAME},{DNAME},{GUID},{PATH}) ON DUPLICATE KEY UPDATE display_name = VALUES(display_name), path = VALUES(path);";
            public static string UPDATE = $@"update client set display_name = {DNAME}, path = {PATH} where id = {ID};";
            public static string GETKEYS = $@"select * from client_keys as c where c.client = {ID} LIMIT 1;";
        }
        
        public class MODULE {
            public static string EXISTS = $@"select m.id from module as m where m.name = {NAME} and m.parent = {PARENT} LIMIT 1;";
            public static string UPSERT = $@"insert into module (parent,name, display_name,hash_guid,path) values ({PARENT}, {NAME},{DNAME},{GUID},{PATH}) ON DUPLICATE KEY UPDATE display_name = VALUES(display_name), path = VALUES(path);";
            public static string UPDATE = $@"update module set display_name = {DNAME}, path = {PATH} where id = {ID};";
        }
    }
}
