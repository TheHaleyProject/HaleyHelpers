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
        Task<Feedback> Delete(IObjectReadRequest input);
        Feedback Exists(IObjectReadRequest input);
        long GetSize(IObjectReadRequest input);
        Task<DirectoryInfoResponse> GetDirectoryInfo(IObjectReadRequest input);
        Task<ObjectCreateResponse> CreateDirectory(IObjectReadRequest input, string rawname);
        Task<Feedback> DeleteDirectory(IObjectReadRequest input, bool recursive);
    }
}
