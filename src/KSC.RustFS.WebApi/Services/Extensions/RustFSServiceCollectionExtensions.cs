using KSC.RustFS.WebApi.Services.Implementations;
using KSC.RustFS.WebApi.Services.Interfaces;

namespace KSC.RustFS.WebApi.Services.Extensions;

public static class RustFSServiceCollectionExtensions
{
    public static IServiceCollection AddStorageService(this IServiceCollection services)
    {
        services.AddScoped<IFileStorageService, RustFsStorageService>();
        return services;
    }
}

