using Microsoft.Extensions.DependencyInjection;
using Sample.MediaInfo.Core.Services;
using Storage.Helper;

namespace Sample.MediaInfo.Core
{
    /// <summary>
    /// Extends <see cref="IServiceCollection"/> to register the services in this project.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds MediaInfo Service and dependencies.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddMediaInfoService(this IServiceCollection services)
        {
            services.AddTransient<IMediaInfoReportService, MediaInfoReportService>();
            services.AddTransient<IMediaInfoProvider, MediaInfoProvider>();

            services.AddAddAzureStorageDefaultAzureTokenCredential();
            services.AddAzureStorageOperations();
        }
    }
}
