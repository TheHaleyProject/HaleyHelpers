using Haley.Abstractions;
using Haley.Enums;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;
using Haley.Utils;
using System.CodeDom;
using System.Security.Cryptography;
using Microsoft.Identity.Client;
using static Haley.Utils.ObjectValidation;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices.ComTypes;

namespace Haley.Services {
    public class DiskStorageService : IDiskStorageService {
        bool _isInitialized = false;
        public DSSConfig Config { get; set; } = new DSSConfig();
        public DiskStorageService(bool write_mode = true):this(null, null,write_mode) { }
        public DiskStorageService(string basePath, bool write_mode = true) : this(basePath, null,write_mode) { }
        public DiskStorageService(string basePath, IDSSIndexing indexer, bool write_mode) {
            BasePath = basePath;
            WriteMode = write_mode;
            //This is supposedly the directory where all storage goes into.
            if (BasePath == null) {
                BasePath = AssemblyUtils.GetBaseDirectory(parentFolder: "DataStore");
            }
            BasePath = BasePath?.ToLower();
            SetIndexer(indexer);
            //If a client is not registered, do we need to register the default client?? and a default module??
        }
        async Task Initialize(bool force = false) {
            if (_isInitialized && !force) return;
            await RegisterClient(new OSSName()); //Registers defaul client
            await RegisterModule(new OSSName(), new OSSName()); //Registers default module
            _isInitialized = true;
        }
        public string BasePath { get; }
        public bool EnableIndexing { get; set; }
        public bool WriteMode { get; set; }
        IDSSIndexing Indexer;

        public string GetBasePath() {
            return BasePath;
        }

        public DiskStorageService SetWriteMode(bool mode) {
            WriteMode = mode;
            return this;
        }

        public IDiskStorageService SetIndexer(IDSSIndexing service) {
            Indexer = service;
            if (Indexer != null) {
                EnableIndexing = true;
                Initialize(true).Wait(); //Once we set the indexer we also try to initialize.
            }
            return this;
        }

        #region Client & Module Management 
        public (string name, string path, Guid guid) GenerateBasePath(OSSName input, string suffix) {
            return OSSUtils.GenerateFileSystemSavePath(input, OSSParseMode.ParseOrGenerate, 2, 5, suffix: suffix, throwExceptions: false);
        }
        public Task<IFeedback> RegisterClient(string name, string password = null) {
            return RegisterClient(new OSSName(name));
        }

        public Task<IFeedback> RegisterModule(string name, string client_name = null) {
            return RegisterModule(new OSSName(name), new OSSName(client_name));
        }
        public async Task<IFeedback> RegisterClient(OSSName input, string password = null) {
            //Password will be stored in the .dss.meta file
            if (input == null) return new Feedback(false, "Name cannot be empty");
            var nameValidation = input.Validate();
            if (!nameValidation.Status) return nameValidation;
            if (input.ControlMode != OSSControlMode.None) input.ControlMode = OSSControlMode.Guid; //Either we allow as is, or we go with GUID. no numbers allowed.
            if (string.IsNullOrWhiteSpace(password)) password = "admin";
            var cInput = GenerateBasePath(input,Config.ClientSuffix); //For client, we only prefer hash mode.
            var path = Path.Combine(BasePath, cInput.path);

            //Thins is we are not allowing any path to be provided by user. Only the name is allowed.

            //Create these folders and then register them.
            if (!Directory.Exists(path) && WriteMode) {
                Directory.CreateDirectory(path); //Create the directory only if write mode is enabled or else, we just try to store the information in cache.
            }

            var signing = RandomUtils.GetString(512);
            var encrypt = RandomUtils.GetString(512);
            var pwdHash = HashUtils.ComputeHash(password, HashMethod.Sha256);
            var result = new Feedback(true, $@"Client {input.DisplayName} is registered");

            var clientInfo = input.MapProperties(new OSSClientInfo(pwdHash, signing, encrypt) { Path = cInput.path, HashGuid = cInput.guid.ToString("N") });
            if (WriteMode) {
                var metaFile = Path.Combine(path, ".client.dss.meta");
                File.WriteAllText(metaFile, clientInfo.ToJson());   // Over-Write the keys here.
            }

            if (!Directory.Exists(path)) result.SetStatus(false).SetMessage("Directory was not created. Check if WriteMode is ON Or make sure proper access is availalbe");

            if (!result.Status || Indexer == null || !EnableIndexing) return result;
            var idxResult = await Indexer.RegisterClient(clientInfo);
            result.Result = idxResult.Result;
            return result;
        }
        public Task<IFeedback> RegisterModule(OSSName input, OSSName client_input, OSSControlMode content_control = OSSControlMode.None, OSSParseMode content_pmode = OSSParseMode.Parse) {
            //AssertValues(true, (client_name,"client name"), (name,"module name")); //uses reflection and might carry performance penalty
            client_input.DisplayName.AssertValue(true, "Client Name");
            input.DisplayName.AssertValue(true, "Module Name");

            var cInput = GenerateBasePath(client_input, Config.ClientSuffix); //For client, we only prefer hash mode.
            return RegisterModule(input, client_input, Path.Combine(BasePath, cInput.path),content_control,content_pmode);
        }
        async Task<IFeedback> RegisterModule(OSSName input, OSSName client_input,string client_path, OSSControlMode content_control = OSSControlMode.None, OSSParseMode content_pmode = OSSParseMode.Parse) {
            //CLIENT INFORMATION BASIC VALIDATION
            client_path.AssertValue(true, "Client Path");
            client_input.DisplayName.AssertValue(true, "Client Name");
            if (!Directory.Exists(client_path)) return new Feedback(false, $@"Directory not found for the client {client_input.DisplayName}");
            if (client_path.Contains("..")) return new Feedback(false, "Client Path contains invalid characters");

            //MODULE INFORMATION BASIC VALIDATION
            if (input == null) return new Feedback(false, "Name cannot be empty");
            var nameValidation = input.Validate();
            if (!nameValidation.Status) return nameValidation;

            var cInput = GenerateBasePath(input,Config.ModuleSuffix); //For client, we only prefer hash mode.
            var path = Path.Combine(client_path, cInput.path); //Including Client Path

            //Create these folders and then register them.
            if (!Directory.Exists(path) && WriteMode) {
                Directory.CreateDirectory(path); //Create the directory.
            }

            var moduleInfo = input.MapProperties(new OSSModuleInfo(client_input.DisplayName) { Path = cInput.path, HashGuid = cInput.guid.ToString("N"),ContentControl = content_control, ContentParse = content_pmode });
            if (WriteMode) {
                var metaFile = Path.Combine(path, ".module.dss.meta");
                File.WriteAllText(metaFile, moduleInfo.ToJson());
            }

            var result = new Feedback(true, $@"Module {input.DisplayName} is registered");
            if (!Directory.Exists(path)) result.SetStatus(false).SetMessage("Directory is not created. Please ensure if the WriteMode is turned ON or proper access is availalbe.");

            if (Indexer == null || !EnableIndexing) return result;
            var idxResult = await Indexer.RegisterModule(moduleInfo);
            result.Result = idxResult.Result;
            return result;

        }
        #endregion

        string FetchBasePath(IOSSRead request) {
            Initialize().Wait(); //To ensure base folders are created.
            string result = BasePath;
            List<string> paths = new List<string>();
            paths.Add(BasePath);

            if (request.Client != null) {
                var info = Indexer.GetClientInfo(request.Client.DisplayName);
                if (info != null) {
                    if (!string.IsNullOrWhiteSpace(info.Path)) paths.Add(info.Path); //Because sometimes we might have modules or clients where we dont' ahve any path specified. So , in those cases, we just ignore them.
                } else if (!string.IsNullOrWhiteSpace(request.Client.DisplayName)) {
                    paths.Add(GenerateBasePath(request.Client,Config.ClientSuffix).path);
                    //Now add this to the client info inside the indexer. //Because we have generated something
                }
            }

            if (request.Module != null) {
                var info = Indexer.GetModuleInfo(request.Module.DisplayName);
                if (info != null) {
                    if (!string.IsNullOrWhiteSpace(info.Path)) paths.Add(info.Path); //Because sometimes we might have modules or clients where we dont' ahve any path specified. So , in those cases, we just ignore them.
                } else if (!string.IsNullOrWhiteSpace(request.Module.DisplayName)) {
                    paths.Add(GenerateBasePath(request.Module,Config.ModuleSuffix).path);
                }
            }
            if (paths.Count > 0) result = Path.Combine(paths.ToArray());

            if (!Directory.Exists(result)) throw new DirectoryNotFoundException("The base path doesn't exists.. Unable to build the base path from given input.");
            return result;
        }

        #region Disk Storage Management 
        public async Task<IOSSResponse> Upload(IOSSWrite input) {
            OSSResponse result = new OSSResponse() {
                Status = false,
                RawName = input.FileOriginalName
            };
            try {
                //The last storage route should be in the format of a file
                if (input.StorageRoutes.Count < 1 || !input.StorageRoutes.Last().IsFile) {
                    //We are trying to upload a file but the last storage route is not in the format of a file.
                    //We need to see if the filestream is present and take the name from there.
                    //Priority for the name comes from TargetName
                    if (!string.IsNullOrWhiteSpace(input.TargetName)) {
                        var tname = Path.GetFileName(input.TargetName);
                        input.StorageRoutes.Add(new OSSRoute(tname, tname.ToDBName(), true, false));
                    } else if (!string.IsNullOrWhiteSpace(input.FileOriginalName)) {
                        var oname = Path.GetFileName(input.FileOriginalName);
                        input.StorageRoutes.Add(new OSSRoute(oname, oname.ToDBName(), true, false));
                    }else if (input.FileStream != null && input.FileStream is FileStream fs) {
                        var fsName = Path.GetFileName(fs.Name);
                        input.StorageRoutes.Add(new OSSRoute(fsName, fsName.ToDBName(), true, false));
                    } else {
                        throw new ArgumentNullException("For the given file no save name is specified.");
                    }
                }

                var bpath = FetchBasePath(input);
                input?.BuildStoragePath(bpath); //This will also ensure we are not trying to delete something 
                if (string.IsNullOrWhiteSpace(input.TargetPath)) {
                    result.Message = "Unable to generate the final storage path. Please check inputs.";
                    return result;
                }

                //What if there is some extension and is missing??
                if (string.IsNullOrWhiteSpace(Path.GetExtension(bpath))) {
                    string exten = string.Empty;
                    //Extension is missing. Lets figure out if we have somewhere. 
                    //Check if target name has it or the origianl filename has it.
                    do {
                        exten = Path.GetExtension(input.TargetName); 
                        if (!string.IsNullOrWhiteSpace(exten)) break;
                        exten = Path.GetExtension(input.FileOriginalName);
                        if (!string.IsNullOrWhiteSpace(exten)) break; 
                        if (input.FileStream != null && input.FileStream is FileStream fs) {
                            exten = Path.GetExtension(fs.Name);
                        }
                    } while (false); //One time event
                    if (!string.IsNullOrWhiteSpace(exten)) {
                        bpath += $@".{exten}";
                    }
                }

                if (input.BufferSize < 4096) input.BufferSize = 4096; //Default CopyTo from System.IO has 80KB buffersize. We setit as 4KB for fast storage.

                if (input.FileStream == null) throw new ArgumentException($@"File stream is null. Nothing to save.");
                input.FileStream.Position = 0; //Precaution

                if (input.TargetPath == bpath) throw new ArgumentException($@"No file save name is processed.");

                if (!ShouldProceedFileUpload(result, input.TargetPath, input.ResolveMode)) return result;

                //Either file doesn't exists.. or exists and replace

                if (!result.ObjectExists || input.ResolveMode == OSSResolveMode.Replace) {
                    //TODO : DEFERRED REPLACEMENT
                    //If the file is currently in use, try for 5 times and then replace. May be easy option would be to store in temporary place and then update a database that a temporary file is created and then later, with some background process check the database and try to replace. This way we dont' have to block the api call or wait for completion.
                    await input.FileStream?.TryReplaceFileAsync(input.TargetPath, input.BufferSize);
                } else if (input.ResolveMode == OSSResolveMode.Revise) {
                    //Then we revise the file and store in same location.
                    //First get the current version name.. and then 
                    if (OSSUtils.PopulateVersionedPath(Path.GetDirectoryName(input.TargetPath),input.TargetPath, out var version_path)){
                        //File exists.. and we also have the name using which we should replace it.
                        //Try copy the file under current name
                        try {
                            //First copy the current file to new version path and then 
                            if (await OSSUtils.TryCopyFileAsync(input.TargetPath, version_path)) {
                                //Copy success
                                await input.FileStream?.TryReplaceFileAsync(input.TargetPath, input.BufferSize);
                            } 
                        } catch (Exception) {
                            await OSSUtils.TryDeleteFile(version_path);
                        }
                    }
                }

                if (!result.ObjectExists) result.Message = "Uploaded."; //For skip also, we will return true (but object will exists)
                result.Status = true;
                result.Size = input.FileStream.Length; //storage size in bytes.

            } catch (Exception ex) {
                result.Message = ex.Message;
                result.Status = false;
            }
            return result;
        }

        public Task<IOSSFileStreamResponse> Download(IOSSRead input, bool auto_search_extension = true) {
            IOSSFileStreamResponse result = new FileStreamResponse() { Status = false, Stream = Stream.Null };
            var path = input?.BuildStoragePath(FetchBasePath(input)); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) return Task.FromResult(result);

            if (!File.Exists(path) && auto_search_extension) {
                //If file extension is not present, then search the targetpath for matching filename and fetch the object (if only one is present).

                if (string.IsNullOrWhiteSpace(Path.GetExtension(path))) {
                    var findName = Path.GetFileNameWithoutExtension(path); //If the extension is not available, obviously it will return only file name. But, what if the file is like 'test.' ends with a period (.). So, we use the GetFileNameWithoutExtension method.

                    //Extension not provided. So, lets to see if we have any matching file.
                    DirectoryInfo dinfo = new DirectoryInfo(Path.GetDirectoryName(path));
                    if (!dinfo.Exists) {
                        result.Message = "The directory doesn't exists.";
                        return Task.FromResult(result);
                    }
                    var matchingFiles = dinfo?.GetFiles()?.Where(p => Path.GetFileNameWithoutExtension(p.Name) == findName).ToList();
                    if (matchingFiles.Count() == 1) {
                        path = matchingFiles.FirstOrDefault().FullName;
                    } else if (matchingFiles.Count() > 1) {
                        //We found mathing items but more than one
                        result.Message = "Multiple matching files found. Please provide a valid extension.";
                        return Task.FromResult(result);
                    }
                }
            }

            if (!File.Exists(path)) {
                result.Message = "File doesn't exist.";
                return Task.FromResult(result);
            }
            result.Status = true;
            result.Extension = Path.GetExtension(path);
            result.Stream = new FileStream(path, FileMode.Open, FileAccess.Read) as Stream;
            return Task.FromResult(result); //Stream is open here.
        }

        public async Task<IFeedback> Delete(IOSSRead input) {
            IFeedback feedback = new Feedback() { Status = false };
            var path = input?.BuildStoragePath(FetchBasePath(input), readonlyMode:true); //This will also ensure we are not trying to create something 
            if (string.IsNullOrWhiteSpace(path)) {
                feedback.Message = "Unable to generate path from provided inputs.";
                return feedback;
            }
           
            if (!File.Exists(path)) {
                feedback.Message = $@"File does not exists : {path}.";
                return feedback;
            }

            feedback.Status = await path.TryDeleteFile();
            feedback.Message = feedback.Status ? "File deleted" : "Unable to delete the file. Check if it is in use by other process & try again.";
            return feedback;
        }

        public IFeedback Exists(IOSSRead input) {
            var feedback = new Feedback() { Status = false };
            var path = input?.BuildStoragePath(FetchBasePath(input), readonlyMode:true); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) {
                feedback.Message = "Unable to generate path from provided inputs.";
                return feedback;
            }
            bool isFile = input.StorageRoutes?.Last().IsFile ?? false; //Why any of the flag?
            //If last storageroute has a IsFile flag, then it means, we are trying to figure out a file existence.
            if (isFile) {
                feedback.Status = File.Exists(path);
            } else {
                feedback.Status = Directory.Exists(path);
            }
            if (!feedback.Status) feedback.Message = $@"Does not exists {path}";
            return feedback;
        }

        public long GetSize(IOSSRead input) {
            var path = input?.BuildStoragePath(FetchBasePath(input)); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return 0;
            return new FileInfo(path).Length;
        }

        public Task<IOSSDirResponse> GetDirectoryInfo(IOSSRead input) {
            IOSSDirResponse result = new DirectoryInfoResponse() { Status = false};

            var path = input?.BuildStoragePath(FetchBasePath(input), readonlyMode:true); //This will also ensure we are not trying to delete something 
            if (string.IsNullOrWhiteSpace(path)) {
                result.Message = "Unable to generate path.";
                return Task.FromResult(result);
            }

            //It is users' responsibility to send a valid path for checking.
            if (!Directory.Exists(path)) {
                result.Message = "Unable to find the specified path. Please check.";
                return Task.FromResult(result);
            }

            var dinfo = new DirectoryInfo(path);

            result.FoldersList = dinfo.GetDirectories()?.Select(p => p.Name)?.ToList();
            result.FilesList = dinfo.GetFiles()?.Select(p => p.Name)?.ToList();
            return Task.FromResult(result);
        }

        public async Task<IOSSResponse> CreateDirectory(IOSSRead input, string rawname) {
            IOSSResponse result = new OSSResponse() {
                Status = false,
                RawName = rawname
            };
            try {
                var path = input?.BuildStoragePath(FetchBasePath(input));  //This will also ensure we are not trying to delete something 
                if (string.IsNullOrWhiteSpace(path)) {
                    result.Message = $@"Unable to generate the path. Please check inputs.";
                    return result;
                }

                if (Directory.Exists(path)) {
                    result.Status = true;
                    result.Message = $@"Directory already exists.";
                    return result;
                }
                if (!(await path?.TryCreateDirectory())) {
                    result.Message = $@"Unable to create the directory. Please check if it is valid.";
                    return result;
                }

                result.Status = true;
                result.Message = "Created";
            } catch (Exception ex) {
                result.Status = false;
                result.Message = ex.Message;
            }
            return result;
        }

        public async  Task<IFeedback> DeleteDirectory(IOSSRead input, bool recursive) {
            IFeedback feedback = new Feedback() { Status = false };
            var path = input?.BuildStoragePath(FetchBasePath(input), readonlyMode: true);
            if (string.IsNullOrWhiteSpace(path)) {
                feedback.Message = "Unable to generate path from provided inputs.";
                return feedback;
            }
            //How do we verfiy, if this the final target that we wish to delete?
            //We should not by mistake end up deleting a wrong directory.
            var expectedToDelete = input.StorageRoutes?.Last().Path?.ToLower().Trim();

            if (string.IsNullOrWhiteSpace(expectedToDelete) ||
                (expectedToDelete == "\\" || expectedToDelete == "/") ||
                expectedToDelete.Equals(BasePath.ToLower())) {
                feedback.Message = "Path is not valid for deleting.";
                return feedback;
            }
            if (!Directory.Exists(path)) {
                feedback.Message = $@"Directory does not exists. : {path}.";
                return feedback;
            }
            //Directory.Delete(path, recursive);
            await path?.TryDeleteDirectory();
            feedback.Status = true;
            feedback.Message = "Deleted successfully";
            return feedback;
        }

        #endregion

        #region Helpers
       

        bool ShouldProceedFileUpload(IOSSResponse result, string filePath, OSSResolveMode conflict) {

            var targetDir = Path.GetDirectoryName(filePath); //Get only the directory.
            //Should we even try to generate the directory first???
            if (!(targetDir?.TryCreateDirectory().Result ?? false)) {
                result.Message = $@"Unable to ensure storage directory. Please check if it is valid. {targetDir}";
                return false;
            }

            if (!filePath.StartsWith(BasePath)) {
                result.Message = "Not authorized for this folder. Please check the path.";
                return false; 
            }

            result.ObjectExists = File.Exists(filePath);
            if (result.ObjectExists) {
                switch (conflict) {
                    case OSSResolveMode.Skip:
                    result.Status = true; 
                    result.Message = "File exists. Skipped";
                    return false; //DONT PROCESS FURTHER
                    case OSSResolveMode.ReturnError:
                    result.Status = false;
                    result.Message = $@"File Exists. Returned Error.";
                    return false; //DONT PROCESS FURTHER
                    case OSSResolveMode.Replace:
                    result.Message = "Replace initiated";
                    return true; //PROCESS FURTHER
                    case OSSResolveMode.Revise:
                    result.Message = "File revision initiated";
                    return true; //PROCESS FURTHER
                }
            }
            return true;
        }

        public Task<IFeedback> AuthorizeClient(object clientInfo, object clientSecret) {
            //Take may be we take the password? no?
            //We can take the password for this client, and compare with the information available in the DB or in the folder. 
            //Whenever indexing is enabled, may be we need to take all the availalbe clients and fetch their password file and update the DB. Because during the time the indexing was down, may be system generated it's own files and stored it.
            IFeedback result = new Feedback();
            result.Status = true;
            result.Message = "No default implementation available. All requests authorized.";
            return Task.FromResult(result);
        }

      
        #endregion
    }
}
