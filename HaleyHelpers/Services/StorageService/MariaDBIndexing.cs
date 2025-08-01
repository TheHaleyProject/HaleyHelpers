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
using static Haley.Internal.IndexingConstant;
using static Haley.Internal.IndexingQueries;

namespace Haley.Utils {
    public class MariaDBIndexing : IStorageIndexingService {
        string _masterCoreFile = "dsscore.sql";
        string _masterClientFile = "dssclient.sql";
        string _key;
        IAdapterGateway _agw;
        bool isValidated = false;
        async Task EnsureValidation() {
            if (!isValidated) await Validate();
        }


        public async Task<IFeedback> RegisterClient(ClientDirectoryInfo info) {
            if (info == null) throw new ArgumentNullException("Input client directory info cannot be null");
            info.Assert();
            //We generate the hash_guid ourselves for the client.
            await EnsureValidation();

            //Check if the client with similar name exists
            var result = await _agw.Scalar(new AdapterArgs(_key) { Query = CLIENTEXISTS }, (NAME, info.Name));
            if (result == null) {
                //Create client
                var thandler = _agw.GetTransactionHandler(_key);
                info.HashGuid = info.Name.CreateGUID(HashMethod.Sha256).ToString(); //No Context added. Check this one later.
                using (thandler.Begin()) {
                    //Register client
                    result = await _agw.Scalar((new AdapterArgs(_key) { Query = ADDCLIENT }).ForTransaction(thandler), (NAME, info.Name),(DNAME,info.DisplayName),  (GUID,info.HashGuid), (PATH, info.Path));
                    if (result != null && result is int clientId) {
                        //Add Info
                        await _agw.NonQuery((new AdapterArgs(_key) { Query = CLIENTKEYS }).ForTransaction(thandler), (ID, clientId), (SIGNKEY, info.SigningKey), (ENCRYPTKEY, info.EncryptKey),(PASSWORD, info.PasswordHash));
                    }
                }
            } else if (result is int cid) {
                //Just update the password.
                await _agw.NonQuery((new AdapterArgs(_key) { Query = CLIENTKEYS }), (ID, cid), (SIGNKEY, info.SigningKey), (ENCRYPTKEY, info.EncryptKey), (PASSWORD, info.PasswordHash));
            }

            if (result != null && result is int cliId) return new Feedback(true) { Result = cliId };

            return new Feedback(false, "Unable to register the client");
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
