using System;
using Haley.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Haley.Models;
using System.IO;

namespace Haley.Abstractions {
    public interface IObjectStorageService {
        //Onus of generating the path doesn't lie with the Storage service.
        //We need Store, Fetch, Delete
        Task<ObjectCreateResponse> Upload(IObjectUploadRequest input);
        Task<StreamResponse> Download(IObjectReadRequest input, bool auto_search_extension = true);
        Task<bool> Delete(IObjectReadRequest input);
        bool Exists(IObjectReadRequest input);
        long GetSize(IObjectReadRequest input);
        Task<DirectoryInfoResponse> GetInfo(IObjectReadRequest input);
        Task<ObjectCreateResponse> CreateRepository(IObjectUploadRequest input);
        Task<bool> DeleteRepository(IObjectReadRequest input, bool recursive);
    }
}
