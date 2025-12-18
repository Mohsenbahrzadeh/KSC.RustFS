using FluentResults;
using KSC.RustFS.WebApi.Services.ServiceModels;

namespace KSC.RustFS.WebApi.Services.Interfaces;
public interface IFileStorageService
{
    Task<Result> CreateBucketIfNotExistsAsync(string bucketName);
    Task<Result<FileUploadResult>> UploadFileAsync(Stream fileStream, string fileName, string bucketName);
    Task<Result<FileListResult>> ListFilesAsync(string bucketName);
    Task<Result<FileDownloadResult>> DownloadFileAsync(string fileName, string bucketName);
}

