using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace ADJL.Services {
    public class MariaDBIndexing : IStorageIndexingService {
        string _masterCoreFile = "dsscore.sql";
        string _masterClientFile = "dssclient.sql";
        string _key;
        IAdapterGateway _agw;
        bool isValidated = false;
        async Task EnsureValidation() {
            if (!isValidated) await Validate();
        }

        public async Task<IFeedback> RegisterClient(string display_name, string password, string path = null) {
            if (string.IsNullOrWhiteSpace(display_name)) throw new ArgumentNullException("Display Name cannot be empty.");
            var name = display_name.ToDBName();
            //We generate the hash_guid ourselves for the client.
            await EnsureValidation();

        }

        public async Task<IFeedback> RegisterClient(string name, string full_name, string password, bool ismanaged=false, string suffix_file = "f", string suffix_dir = "d") {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
            await EnsureValidation();
            var path = ManagedNameUtils.GetBasePath(name, ismanaged);
            return await UpsertClient(path.name, full_name,password, path.guid, path.path, suffix_file, suffix_dir);
        }

        public async Task<IFeedback> RegisterClient(string name, string full_name, string password, Guid guid, string path, string suffix_file = "f", string suffix_dir = "d") {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
            await EnsureValidation();
            //Check if the client with similar name exists
            var result = await _agw.Scalar(new AdapterArgs(_key) { Query = CLIENTEXISTS }, (NAME, name));
            if (result == null) {
                //Create client
                var thandler = _agw.GetTransactionHandler(_key);
                using (thandler.Begin()) {
                    //Register client
                    result = await _agw.Scalar((new AdapterArgs(_key) { Query = ADDCLIENT }).ForTransaction(thandler), (NAME, name), (GUID, guid.ToString()), (PATH, path),(PASSWORD,password));
                    if (result != null && result is int clientId) {
                        //Add Info
                        await _agw.NonQuery((new AdapterArgs(_key) { Query = ADDCLIENTINFO }).ForTransaction(thandler), (ID, clientId), (SUFFIX_FILE, suffix_file), (SUFFIX_DIR, suffix_dir), (FULLNAME, full_name));
                        //Add Signing Keys
                        await _agw.NonQuery((new AdapterArgs(_key) { Query = ADDENCRYPTION }).ForTransaction(thandler), (ID, clientId), (SIGNKEY, RandomUtils.GetString(256)), (ENCRYPTKEY, RandomUtils.GetString(256)));
                    }
                }
            } else {
                //Just update the password.
                var pwd = await _agw.Scalar(new AdapterArgs(_key) { Query = CLIENTPASS }, (ID, (int)result));
                if (pwd == null || pwd.ToString() != password) {
                    //Update the password with new value
                    await _agw.NonQuery(new AdapterArgs(_key) { Query = CLIENTUPDATEPASS }, (VALUE, password), (ID, (int)result));
                }
            }

            if (result != null && result is int cliId) return new Feedback(true) { Result = cliId };
           
            return new Feedback(false,"Unable to register the client"); 
        }

        public async Task Validate() {
            try {
                //If the service or the db doesn't exist, we throw exception or else the system would assume that nothing is wrong. If they wish , they can still turn of the indexing.
                if (!_agw.ContainsKey(_key)) throw new ArgumentException($@"Storage Indexing service validation failure.No adapter found for the given key {_key}");
                //Next step is to find out if the database exists or not? Should we even try to check if the database exists or directly run the sql script and create the database if it doesn't exists?
                var sqlFile = Path.Combine(AssemblyUtils.GetBaseDirectory(), "Resources", _masterCoreFile);
                if (!File.Exists(sqlFile)) throw new ArgumentException($@"Master sql file for creating the storage DB is not found. Please check : {_masterCoreFile}");
                //if the file exists, then run this file against the adapter gateway but ignore the db name.
                var content = File.ReadAllText(sqlFile);
                //We know that the file itself contains "dss_core" as the schema name. Replace that with new one.
                var dbname = _agw[_key].Info?.DBName ?? "mss_core"; //This is supposedly our db name.
                content = content.Replace("dss_core", dbname);
                //?? Should we run everything in one go or run as separate statements???
                await _agw.NonQuery(new AdapterArgs(_key) { ExcludeDBInConString = true, Query = content });
                isValidated = true;
            } catch (Exception ex) {
                throw ex;
            }
           
        }

        public MariaDBIndexing(IAdapterGateway agw, string key) {
            _key = key;
            _agw = agw;
        }
    }
}
