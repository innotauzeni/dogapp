using Dog.Application.Common;
using Dog.Application.Contracts;
using Dog.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dog.Application
{
    /// <summary>
    /// Registers Cloudflare R2 file storage. Called by both the API and the
    /// Portal so they share one implementation and config-loading rules.
    /// </summary>
    public static class FileStorageRegistration
    {
        public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
        {
            var options = R2Options.Load(configuration);
            services.AddSingleton(options);
            services.AddSingleton<IFileStorageService, R2FileStorageService>();
            return services;
        }
    }
}
