using KSC.RustFS.WebApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KSC.RustFS.WebApi.Controllers
{
    [ApiController]
    [Route("api/FileManagement")]
    public class FileManagementController : ControllerBase
    {
        private readonly IFileStorageService _storageService;
        private const string BucketName = "chatbot-files";

        public FileManagementController(IFileStorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("فایلی آپلود نشده است.");

            await _storageService.CreateBucketIfNotExistsAsync(BucketName);

            await using var stream = file.OpenReadStream();
            var result = await _storageService.UploadFileAsync(stream, file.FileName, BucketName);

            if (result.IsFailed)
                return BadRequest(new { Error = result.Errors.First().Message });

            return Ok(new { result.Value.FileUrl, result.Value.FileName });
        }

        [HttpGet("list-files")]
        public async Task<IActionResult> ListFiles()
        {
            var bucketResult = await _storageService.CreateBucketIfNotExistsAsync(BucketName);
            if (bucketResult.IsFailed)
                return StatusCode(500, bucketResult.Errors.First().Message);

            var result = await _storageService.ListFilesAsync(BucketName);

            if (result.IsFailed)
                return BadRequest(new { Error = result.Errors.First().Message });

            return Ok(result.Value.FileNames);
        }

        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var bucketResult = await _storageService.CreateBucketIfNotExistsAsync(BucketName);
            if (bucketResult.IsFailed)
                return StatusCode(500, bucketResult.Errors.First().Message);

            var result = await _storageService.DownloadFileAsync(fileName, BucketName);

            if (result.IsFailed)
            {
                return result.Errors.Any(e => e.Message.Contains("یافت نشد"))
                    ? NotFound(result.Errors.First().Message)
                    : BadRequest(result.Errors.First().Message);
            }

            var download = result.Value;
            return File(download.FileStream, download.ContentType, download.FileName);
        }
    }
}
