using Haley.Abstractions;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Haley.Internal.IndexingQueries;
using static Haley.Utils.ObjectValidation;

namespace Haley.Services {
    public class DiskStorageService : IDiskStorageService {
        bool _isInitialized = false;
        const string METAFILE = ".dss.meta";
        const string CLIENTMETAFILE = ".client" + METAFILE;
        const string MODULEMETAFILE = ".module" + METAFILE;
        const string WORKSPACEMETAFILE = ".ws" + METAFILE;
        const string DEFAULTPWD = "admin";
        public IDSSConfig Config { get; set; } = new DSSConfig();
        public DiskStorageService(bool write_mode = true):this(null,null, write_mode) { }
        public DiskStorageService(string basePath, bool write_mode = true) : this(basePath, write_mode,null) { }
        public DiskStorageService(IAdapterGateway agw, string adapter_key, bool write_mode = true) : this(null, write_mode, new MariaDBIndexing(agw, adapter_key)) { }
        public DiskStorageService(IAdapterGateway agw, string adapter_key, string basePath, bool write_mode =true) : this(basePath, write_mode, new MariaDBIndexing(agw,adapter_key)) { }
        public DiskStorageService(string basePath, bool write_mode, IDSSIndexing indexer) {
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
            await RegisterClient(new OSSControlled()); //Registers defaul client
            await RegisterModule(new OSSControlled(), new OSSControlled()); //Registers default module
            _isInitialized = true;
        }
        public string BasePath { get; }
        public bool WriteMode { get; set; }
        IDSSIndexing Indexer;

      
        public DiskStorageService SetWriteMode(bool mode) {
            WriteMode = mode;
            return this;
        }
        public IDiskStorageService SetIndexer(IDSSIndexing service) {
            Indexer = service;
            return this;
        }

        #region Client & Module Management 
        public (string name, string path) GenerateBasePath(IOSSControlled input, OSSComponent basePath) {
            string suffix = string.Empty;
            int length = 2;
            int depth = 0;
            switch (basePath) {
                case OSSComponent.Client:
                suffix = Config.SuffixClient;
                length = 4; depth = 2;
                break;
                case OSSComponent.Module:
                suffix = Config.SuffixModule;
                length = 5; depth = 2;
                break;
                case OSSComponent.WorkSpace:
                var suffixAddon = input.ControlMode == OSSControlMode.None ? "u" : "m";
                suffix = suffixAddon + Config.SuffixWorkSpace;
                length = 1; depth = 4;
                break;
                case OSSComponent.File:
                suffix = Config.SuffixFile;
                break;
            }
            return OSSUtils.GenerateFileSystemSavePath(input, OSSParseMode.ParseOrGenerate, (n) => { return (length, depth); }, suffix: suffix, throwExceptions: false);
        }
        public Task<IFeedback> RegisterClient(string name, string password = null) {
            return RegisterClient(new OSSControlled(name));
        }
        public Task<IFeedback> RegisterModule(string name, string client_name = null) {
            return RegisterModule(new OSSControlled(name), new OSSControlled(client_name));
        }
        public async Task<IFeedback> RegisterClient(IOSSControlled input, string password = null) {
            //Password will be stored in the .dss.meta file
            if (input == null) return new Feedback(false, "Name cannot be empty");
            if (!input.TryValidate(out var msg)) return new Feedback(false, msg);
            if (input.ControlMode != OSSControlMode.None) input.ControlMode = OSSControlMode.Guid; //Either we allow as is, or we go with GUID. no numbers allowed.
            if (string.IsNullOrWhiteSpace(password)) password = DEFAULTPWD;
            var cInput = GenerateBasePath(input,OSSComponent.Client); //For client, we only prefer hash mode.
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

            var clientInfo = input.MapProperties(new OSSClient(pwdHash, signing, encrypt) { Path = cInput.path});
            if (WriteMode) {
                var metaFile = Path.Combine(path,CLIENTMETAFILE);
                File.WriteAllText(metaFile, clientInfo.ToJson());   // Over-Write the keys here.
            }

            if (!Directory.Exists(path)) result.SetStatus(false).SetMessage("Directory was not created. Check if WriteMode is ON Or make sure proper access is availalbe");

            if (!result.Status || Indexer == null ) return result;
            var idxResult = await Indexer.RegisterClient(clientInfo);
            result.Result = idxResult.Result;
            return result;
        }
        public async Task<IFeedback> RegisterModule(IOSSControlled input, IOSSControlled client) {
            //AssertValues(true, (client_name,"client name"), (name,"module name")); //uses reflection and might carry performance penalty
            string msg = string.Empty;
            if (!input.TryValidate(out msg)) new Feedback(false, msg);
            if (!client.TryValidate(out msg)) new Feedback(false, msg);

            var client_path = GenerateBasePath(client, OSSComponent.Client).path; //For client, we only prefer hash mode.
            if (!Directory.Exists(client_path)) return new Feedback(false, $@"Directory not found for the client {client.DisplayName}");
            if (client_path.Contains("..")) return new Feedback(false, "Client Path contains invalid characters");

            //MODULE INFORMATION BASIC VALIDATION
            var modPath = GenerateBasePath(input, OSSComponent.Module).path; //For client, we only prefer hash mode.
            var path = Path.Combine(BasePath, client_path, modPath); //Including Client Path

            //Create these folders and then register them.
            if (!Directory.Exists(path) && WriteMode) {
                Directory.CreateDirectory(path); //Create the directory.
            }

            var moduleInfo = input.MapProperties(new OSSModule(client.Name) { Path = modPath});
            if (WriteMode) {
                var metaFile = Path.Combine(path, MODULEMETAFILE);
                File.WriteAllText(metaFile, moduleInfo.ToJson());
            }

            var result = new Feedback(true, $@"Module {input.DisplayName} is registered");
            if (!Directory.Exists(path)) result.SetStatus(false).SetMessage("Directory is not created. Please ensure if the WriteMode is turned ON or proper access is availalbe.");

            if (Indexer == null) return result;
            var idxResult = await Indexer.RegisterModule(moduleInfo);
            result.Result = idxResult.Result;
            return result;
        }
        public Task<IFeedback> RegisterWorkSpace(string name, string client_name = null, string module_name = null) {
            return RegisterWorkSpace(name, client_name, module_name);
        }
        public Task<IFeedback> RegisterWorkSpace(string name, string client_name , string module_name , OSSControlMode content_control = OSSControlMode.None, OSSParseMode content_pmode = OSSParseMode.Parse) {
            return RegisterWorkSpace(new OSSControlled(name,OSSControlMode.Guid,OSSParseMode.ParseOrGenerate),new OSSControlled(client_name),new OSSControlled(module_name),content_control,content_pmode);
        }
        public async Task<IFeedback> RegisterWorkSpace(IOSSControlled input, IOSSControlled client, IOSSControlled module, OSSControlMode content_control = OSSControlMode.None, OSSParseMode content_pmode = OSSParseMode.Parse) {
            string msg = string.Empty;
            if (!input.TryValidate(out msg)) throw new Exception(msg);
            if (!client.TryValidate(out msg)) throw new Exception(msg);
            if (!module.TryValidate(out msg)) throw new Exception(msg);

            var cliPath = GenerateBasePath(client, OSSComponent.Client).path; 
            var modPath = GenerateBasePath(module, OSSComponent.Module).path;

            var bpath = Path.Combine(BasePath, cliPath, modPath);
            if (!Directory.Exists(bpath)) return new Feedback(false, $@"Unable to lcoate the basepath for the Client : {client.DisplayName}, Module : {module.DisplayName}");
            if (bpath.Contains("..")) return new Feedback(false, "Invalid characters found in the base path.");

            //MODULE INFORMATION BASIC VALIDATION
            var wsPath = GenerateBasePath(input, OSSComponent.WorkSpace).path; //For client, we only prefer hash mode.
            var path = Path.Combine(bpath, wsPath); //Including Base Paths

            //Create these folders and then register them.
            if (!Directory.Exists(path) && WriteMode) {
                Directory.CreateDirectory(path); //Create the directory.
            }

            var wsInfo = input.MapProperties(new OSSWorkspace(client.Name,module.Name,input.DisplayName) { Path = wsPath });
            if (WriteMode) {
                var metaFile = Path.Combine(path, WORKSPACEMETAFILE);
                File.WriteAllText(metaFile, wsInfo.ToJson());
            }

            var result = new Feedback(true, $@"Workspace {input.DisplayName} is registered");
            if (!Directory.Exists(path)) result.SetStatus(false).SetMessage("Directory is not created. Please ensure if the WriteMode is turned ON or proper access is availalbe.");

            if (Indexer == null) return result;
            var idxResult = await Indexer.RegisterWorkspace(wsInfo);
            result.Result = idxResult.Result;
            return result;
        }
        #endregion

        #region Path Processing
        public string GetStorageRoot() {
            return BasePath;
        }
        void FetchClientPath(IOSSRead request, List<string> paths) {
            if (paths == null) paths = new List<string>();
            if (request.Client != null) {
                var info = Indexer?.GetClientInfo(request.Client.Name);
                if (info != null) {
                    if (!string.IsNullOrWhiteSpace(info.Path)) paths.Add(info.Path); //Because sometimes we might have modules or clients where we dont' ahve any path specified. So , in those cases, we just ignore them.
                } else if (!string.IsNullOrWhiteSpace(request.Client.Name)) {
                    var tuple = GenerateBasePath(request.Client, OSSComponent.Client); //here, we are merely generating a path based on what the user has provided. It doesn't mean that such a path really exists . 
                    paths.Add(tuple.path);
                    //First verify if the path exists.. If yes, then try to read the file from there.
                    try {
                        var metafile = Path.Combine(BasePath, tuple.path, CLIENTMETAFILE);
                        if (File.Exists(metafile)) {
                            //File exists , gives us the password, encrypt key and everything.. if not available already in the database cache.
                            var cInfo = File.ReadAllText(metafile).FromJson<OSSClient>();
                            if (cInfo != null) {
                                Indexer?.TryAddInfo(cInfo);
                            }
                        }
                    } catch (Exception) {
                    }
                }
            }
        }

        void FetchModulePath(IOSSRead request, List<string> paths) {
            if (paths == null) paths = new List<string>();
            if (request.Module != null) {
                if (Indexer?.TryGetComponentInfo(Indexer.try))
                var info = Indexer?.GetModuleInfo(request.Module.Name, request.Client.Name);
                if (info != null) {
                    if (!string.IsNullOrWhiteSpace(info.Path)) paths.Add(info.Path); //Because sometimes we might have modules or clients where we dont' ahve any path specified. So , in those cases, we just ignore them.
                } else if (!string.IsNullOrWhiteSpace(request.Module.DisplayName)) {
                    var tuple = GenerateBasePath(request.Module, OSSComponent.Module); //here, we are merely generating a path based on what the user has provided. It doesn't mean that such a path really exists . 
                    paths.Add(tuple.path);
                    //First verify if the path exists.. If yes, then try to read the file from there.
                    try {
                        var metafile = Path.Combine(BasePath, tuple.path, MODULEMETAFILE);
                        if (File.Exists(metafile)) {
                            //File exists , gives us the password, encrypt key and everything.. if not available already in the database cache.
                            var cInfo = File.ReadAllText(metafile).FromJson<OSSModule>();
                            if (cInfo != null) {
                                Indexer?.TryAddInfo(cInfo);
                            }
                        }
                    } catch (Exception) {
                    }
                }
            }
        }
        string FetchBasePath(IOSSRead request) {
            Initialize().Wait(); //To ensure base folders are created.
            string result = BasePath;
            List<string> paths = new List<string>();
            paths.Add(BasePath);
            FetchClientPath(request, paths);
            FetchModulePath(request, paths);

            if (paths.Count > 0) result = Path.Combine(paths.ToArray());
            if (!Directory.Exists(result)) throw new DirectoryNotFoundException("The base path doesn't exists.. Unable to build the base path from given input.");
            return result;
        }
        public (int length, int depth) SplitProvider(bool isNumber) {
            if (isNumber) return (Config.SplitLengthNumber, Config.DepthNumber);
            return (Config.SplitLengthHash, Config.DepthHash);
        }
        public void EnsureStorageRoutes(IOSSRead input) {
            //The last storage route should be in the format of a file
            if (input.StorageRoutes.Count < 1 || !input.StorageRoutes.Last().IsFile) {
                //We are trying to upload a file but the last storage route is not in the format of a file.
                //We need to see if the filestream is present and take the name from there.
                //Priority for the name comes from TargetName
                string targetFileName = string.Empty;
                string targetFilePath = string.Empty;
                if (!string.IsNullOrWhiteSpace(input.TargetName)) {
                    targetFileName = Path.GetFileName(input.TargetName);
                } else if (input is IOSSWrite inputW) {
                    if (!string.IsNullOrWhiteSpace(inputW.FileOriginalName)) {
                        targetFileName = Path.GetFileName(inputW.FileOriginalName);
                    } else if (inputW.FileStream != null && inputW.FileStream is FileStream fs) {
                        targetFileName = Path.GetFileName(fs.Name);
                    }
                } else {
                    throw new ArgumentNullException("For the given file no save name is specified.");
                }

                //Now, this targetFileName, may or may not be split based on what was defined in the module.
                //Check the module info.
                var mInfo = Indexer?.GetModuleInfo(input.Module.Name, input.Client.Name);
                if (mInfo != null) {
                    //TODO: USE THE INDEXER TO GET THE PATH FOR THIS SPECIFIC FILE WITH MODULE AND CLIENT NAME.
                    //TODO: IF THE PATH IS OBTAINED, THEN JUST JOIN THE PATHS.
                    //targetFilePath = OSSUtils.GenerateFileSystemSavePath(new OSSCtrld(targetFileName, mInfo.ContentControl, mInfo.ContentParse), splitProvider: SplitProvider, suffix: Config.SuffixFile, throwExceptions: true).path;
                } else {
                    targetFilePath = targetFileName.ToDBName(); //Just lower it 
                }
                input.StorageRoutes.Add(new OSSRoute(targetFileName, targetFilePath, true, false));
            }
        }
        public (string basePath,string targetPath) ProcessAndBuildStoragePath(IOSSRead input,bool ensureFileRoute = true, bool allowRootAccess = false, bool readonlyMode = false) {
            var bpath = FetchBasePath(input);
            if (ensureFileRoute) EnsureStorageRoutes(input);
            var path = input?.BuildStoragePath(bpath,allowRootAccess,readonlyMode); //This will also ensure we are not trying to delete something 
            return (bpath, path);
        }
        #endregion

        #region Disk Storage Management 
        public async Task<IOSSResponse> Upload(IOSSWrite input) {
            OSSResponse result = new OSSResponse() {
                Status = false,
                RawName = input.FileOriginalName
            };
            try {
                if (!WriteMode) {
                    result.Message = "Application is in Read-Only mode.";
                    return result;
                }
                var gPaths = ProcessAndBuildStoragePath(input,ensureFileRoute:true);
                if (string.IsNullOrWhiteSpace(input.TargetPath)) {
                    result.Message = "Unable to generate the final storage path. Please check inputs.";
                    return result;
                }

                //What if there is some extension and is missing??
                if (string.IsNullOrWhiteSpace(Path.GetExtension(gPaths.basePath))) {
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
                        gPaths.targetPath += $@"{exten}";
                    }
                }

                if (input.BufferSize < 4096) input.BufferSize = 4096; //Default CopyTo from System.IO has 80KB buffersize. We setit as 4KB for fast storage.

                if (input.FileStream == null) throw new ArgumentException($@"File stream is null. Nothing to save.");
                input.FileStream.Position = 0; //Precaution

                if (input.TargetPath == gPaths.basePath) throw new ArgumentException($@"No file save name is processed.");

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
            var path = ProcessAndBuildStoragePath(input,ensureFileRoute: true,readonlyMode:true).targetPath;
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
            if (!WriteMode) {
                feedback.Message = "Application is in Read-Only mode.";
                return feedback;
            }
            var path = ProcessAndBuildStoragePath(input, ensureFileRoute: true, readonlyMode:true).targetPath;

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
        public IFeedback Exists(IOSSRead input, bool isFilePath = false) {
            var feedback = new Feedback() { Status = false };
            var path = ProcessAndBuildStoragePath(input, ensureFileRoute: isFilePath, readonlyMode: true).targetPath;
            if (string.IsNullOrWhiteSpace(path)) {
                feedback.Message = "Unable to generate path from provided inputs.";
                return feedback;
            }
            if (isFilePath) {
                feedback.Status = File.Exists(path);
            } else {
                feedback.Status = Directory.Exists(path);
            }
            if (!feedback.Status) feedback.Message = $@"Does not exists {path}";
            return feedback;
        }

        public long GetSize(IOSSRead input) {
            var path = ProcessAndBuildStoragePath(input,ensureFileRoute:true, readonlyMode: true).targetPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return 0;
            return new FileInfo(path).Length;
        }

        public Task<IOSSDirResponse> GetDirectoryInfo(IOSSRead input) {
            IOSSDirResponse result = new OSSDirResponse() { Status = false};
            var path = ProcessAndBuildStoragePath(input, ensureFileRoute: false, readonlyMode: true).targetPath;
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
                if (!WriteMode) {
                    result.Message = "Application is in Read-Only mode.";
                    return result;
                }
                var path = ProcessAndBuildStoragePath(input, ensureFileRoute: false, readonlyMode: true).targetPath;

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
            if (!WriteMode) {
                feedback.Message = "Application is in Read-Only mode.";
                return feedback;
            }
            var path = ProcessAndBuildStoragePath(input, readonlyMode: true).targetPath;
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
