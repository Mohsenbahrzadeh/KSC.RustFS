namespace KSC.RustFS.WebApi.Services.ServiceModels;
public record FileDownloadResult(Stream FileStream, string ContentType, string FileName);
