using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FluentResults;
using KSC.RustFS.WebApi.Services.Interfaces;
using KSC.RustFS.WebApi.Services.ServiceModels;

namespace KSC.RustFS.WebApi.Services.Implementations;


public class RustFsStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<RustFsStorageService> _logger;

    public RustFsStorageService(ILogger<RustFsStorageService> logger)
    {
        _logger = logger;

        var credentials = new BasicAWSCredentials("rustfsadmin", "rustfsadmin"); 
        var config = new AmazonS3Config
        {
            ServiceURL = "http://localhost:9000", 
            ForcePathStyle = true
        };
        _s3Client = new AmazonS3Client(credentials, config);
    }

    public async Task<Result> CreateBucketIfNotExistsAsync(string bucketName)
    {
        try
        {
            var listResponse = await _s3Client.ListBucketsAsync();
            if (listResponse.Buckets.Any(b => b.BucketName == bucketName))
            {
                return Result.Ok();
            }

            await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
            _logger.LogInformation($"Bucket '{bucketName}' created successfully.");
            return Result.Ok();
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, $"S3 error while creating bucket '{bucketName}': {ex.Message}");
            return Result.Fail($"خطا در ایجاد bucket: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while creating bucket '{bucketName}'");
            return Result.Fail("خطای غیرمنتظره در ایجاد bucket");
        }
    }

    public async Task<Result<FileUploadResult>> UploadFileAsync(Stream fileStream, string fileName, string bucketName)
    {
        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = fileName,
                InputStream = fileStream,
                AutoCloseStream = false
            };

            var response = await _s3Client.PutObjectAsync(putRequest);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                return Result.Fail<FileUploadResult>("آپلود فایل با خطا مواجه شد.");
            }

            string fileUrl = $"http://localhost:9000/{bucketName}/{fileName}";
            _logger.LogInformation($"File '{fileName}' uploaded successfully to '{fileUrl}'.");

            return Result.Ok(new FileUploadResult(fileUrl, fileName));
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, $"S3 error uploading file '{fileName}'");
            return Result.Fail<FileUploadResult>($"خطای S3 در آپلود: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error uploading file '{fileName}'");
            return Result.Fail<FileUploadResult>("خطای غیرمنتظره در آپلود فایل");
        }
    }

    public async Task<Result<FileListResult>> ListFilesAsync(string bucketName)
    {
        try
        {
            var request = new ListObjectsV2Request { BucketName = bucketName };
            var response = await _s3Client.ListObjectsV2Async(request);

            var fileNames = response.S3Objects.Select(o => o.Key).ToList().AsReadOnly();

            _logger.LogInformation($"Listed {fileNames.Count} files in bucket '{bucketName}'.");
            return Result.Ok(new FileListResult(fileNames));
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, $"S3 error listing files in bucket '{bucketName}'");
            return Result.Fail<FileListResult>($"خطای S3 در لیست فایل‌ها: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error listing files");
            return Result.Fail<FileListResult>("خطای غیرمنتظره در لیست فایل‌ها");
        }
    }

    public async Task<Result<FileDownloadResult>> DownloadFileAsync(string fileName, string bucketName)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = fileName
            };

            var response = await _s3Client.GetObjectAsync(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                return Result.Fail<FileDownloadResult>("دانلود فایل با خطا مواجه شد.");
            }

            // تشخیص ContentType ساده
            string contentType = Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };

            _logger.LogInformation($"File '{fileName}' downloaded successfully.");

            return Result.Ok(new FileDownloadResult(response.ResponseStream, contentType, fileName));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result.Fail<FileDownloadResult>("فایل مورد نظر یافت نشد.");
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, $"S3 error downloading file '{fileName}'");
            return Result.Fail<FileDownloadResult>($"خطای S3 در دانلود: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error downloading file '{fileName}'");
            return Result.Fail<FileDownloadResult>("خطای غیرمنتظره در دانلود فایل");
        }
    }


}

