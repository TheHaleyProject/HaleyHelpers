using System;
using Haley.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;
using System.IO;

namespace Haley.Abstractions {
    public interface IStorageService {
        //We need Store, Fetch, Delete
        #region Vault
        Task<FileStorageSummary> Upload(StorageRequest input, Stream file, int bufferSize = 8192);
        Task<StorageStreamResponse> Download(StorageRequestBase input);
        Task<bool> Delete(StorageRequestBase input);
        bool Exists(StorageRequest input);
        long GetSize(StorageRequestBase input);
        Task<StorageSummary> CreateRepository(StorageRequest input);
        Task<bool> DeleteRepository(StorageRequestBase input, bool recursive);
        #endregion

        #region Repository
        Task<FileStorageSummary> UploadToRepo(RepoStorageRequest input, Stream file, int bufferSize = 8192);
        Task<RepoSummary> ReadRepoInfo(RepoStorageRequestBase input);
        Task<Stream> DownloadFromRepo(RepoStorageRequestBase input);
        Task<StorageResponseBase> DeleteFromRepo(RepoStorageRequestBase input);
        Task<StorageResponseBase> CreateFolderInRepo(RepoStorageRequestBase input);
        Task<StorageResponseBase> DeleteFolderInRepo(RepoStorageRequestBase input, bool recursive);

        #endregion
    }
}
