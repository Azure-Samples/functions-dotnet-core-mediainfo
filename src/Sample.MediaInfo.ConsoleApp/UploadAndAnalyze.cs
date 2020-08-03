using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Sample.MediaInfo.Core;
using Storage.Helper;

namespace Sample.MediaInfo.ConsoleApp
{
    /// <summary>
    /// Performs an upload and a remote analysis of the sample media file.
    /// </summary>
    public class UploadAndAnalyze : IUploadAndAnalyze
    {
        private readonly IMediaInfoReportService _mediaInfoReportService;
        private readonly IAzureStorageOperations _azureStorageOperations;
        private readonly ILogger<UploadAndAnalyze> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadAndAnalyze"/> class.
        /// </summary>
        /// <param name="mediaInfoReportService">Injected <see cref="IMediaInfoReportService"/>.</param>
        /// <param name="azureStorageOperations">Injected <see cref="IAzureStorageOperations"/>.</param>
        /// <param name="logger">Injected <see cref="ILogger"/>.</param>
        public UploadAndAnalyze(
            IMediaInfoReportService mediaInfoReportService,
            IAzureStorageOperations azureStorageOperations,
            ILogger<UploadAndAnalyze> logger)
        {
            _mediaInfoReportService = mediaInfoReportService;
            _azureStorageOperations = azureStorageOperations;
            _logger = logger;
        }

        /// <summary>
        /// Performs an upload and a remote analysis of the sample media file.
        /// </summary>
        /// <param name="sourcePath">The source file path.</param>
        /// <param name="accountName">The account name to upload to.</param>
        /// <param name="container">The container to upload to.</param>
        /// <returns>The MediaInfo report.</returns>
        public async Task<JObject> ExecuteAsync(string sourcePath, string accountName, string container)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentNullException(nameof(sourcePath));
            }

            if (!File.Exists(sourcePath))
            {
                var msg = $"Attempted to use nonexistent file: {sourcePath} as input.";
                _logger.LogError(msg);
                throw new ArgumentException(msg, nameof(container));
            }

            if (string.IsNullOrEmpty(Path.GetFileName(sourcePath)))
            {
                var msg = $"{nameof(sourcePath)} does not end with a filename: {sourcePath}";
                _logger.LogError(msg);
                throw new ArgumentException(msg, nameof(container));
            }

            if (string.IsNullOrEmpty(accountName))
            {
                throw new ArgumentNullException(nameof(accountName));
            }

#pragma warning disable CA1308 // Normalize strings to uppercase
            if (accountName != accountName.ToLowerInvariant())
#pragma warning restore CA1308 // Normalize strings to uppercase
            {
                var msg = $"Account names should be lower case: {accountName}";
                throw new ArgumentException(msg, nameof(accountName));
            }

            if (string.IsNullOrEmpty(container))
            {
                throw new ArgumentNullException(nameof(container));
            }

            var containerUri = new Uri($"https://{accountName}.blob.core.windows.net/{container}");
            _ = await _azureStorageOperations.ContainerCreateIfNotExistsAsync(containerUri).ConfigureAwait(false);

            var blobUri = new Uri($"https://{accountName}.blob.core.windows.net/{container}/{Path.GetFileName(sourcePath)}");
            _ = await _azureStorageOperations.BlobUploadAsync(sourcePath, blobUri).ConfigureAwait(false);

            var report = await _mediaInfoReportService.GetMediaInfoCompleteInformForUriAsync(blobUri).ConfigureAwait(false);

            return report;
        }
    }
}
