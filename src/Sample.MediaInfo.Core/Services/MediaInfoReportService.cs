using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Storage.Helper;
using Storage.Helper.Models;

namespace Sample.MediaInfo.Core.Services
{
    /// <summary>
    /// Generates MediaInfo reports.
    /// </summary>
    public class MediaInfoReportService : IMediaInfoReportService
    {
        /// <summary>
        /// When MediaInfo's OpenBufferContinueGoToGet returns this value, it means it wants to read forward.
        /// </summary>
        public const long MediaInfoReadForward = -1;
        private const string CompleteOptionString = "Complete";
        private const string CompleteOptionValueString = "1";
        private const string OutputOptionString = "Output";
        private const string JsonOutputValueString = "JSON";
        private readonly IMediaInfoProvider _mediaInfoProvider;
        private readonly IAzureStorageOperations _azureStorageOperations;
        private readonly IAzureStorageReadByteRangeOperations _azureStorageReadByteRangeOperations;
        private readonly ILogger<MediaInfoReportService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoReportService"/> class.
        /// </summary>
        /// <param name="mediaInfoProvider">Injected <see cref="IMediaInfoProvider"/>.</param>
        /// <param name="azureStorageOperations">Injected <see cref="IAzureStorageOperations"/>.</param>
        /// <param name="azureStorageReadByteRangeOperations">Injected <see cref="IAzureStorageReadByteRangeOperations"/>.</param>
        /// <param name="logger">Injected <see cref="ILogger"/>.</param>
        public MediaInfoReportService(
            IMediaInfoProvider mediaInfoProvider,
            IAzureStorageOperations azureStorageOperations,
            IAzureStorageReadByteRangeOperations azureStorageReadByteRangeOperations,
            ILogger<MediaInfoReportService> logger)
        {
            _mediaInfoProvider = mediaInfoProvider;
            _azureStorageOperations = azureStorageOperations;
            _azureStorageReadByteRangeOperations = azureStorageReadByteRangeOperations;
            _logger = logger;
        }

        /// <summary>
        /// The MediaInfoStatus
        /// Ref: https://github.com/MediaArea/MediaInfoLib/blob/master/Source/MediaInfoDLL/MediaInfoDLL.cs#L63
        /// .
        /// </summary>
        [Flags]
        private enum MediaInfoStatus
        {
            None = 0x00,
            Accepted = 0x01,
            Filled = 0x02,
            Updated = 0x04,
            Finalized = 0x08,
        }

        /// <inheritdoc/>
        public async Task<JObject> GetMediaInfoCompleteInformForUriAsync(Uri blobUri)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));

            // Get MediaInfo instance
            var mediaInfoLib = _mediaInfoProvider.GetMediaInfoLib();

            // Get contentLength
            var contentLength = await _azureStorageOperations.GetBlobContentLengthAsync(blobUri).ConfigureAwait(false);
            if (contentLength == 0)
            {
                var message = $"Content length 0 for blob uri {blobUri}";
                _logger.LogError(message);
                throw new Exception(message);
            }

            long desiredOffset = 0;
            do
            {
                // Get content for current desiredOffset
                CachedHttpRangeContent cachedHttpRangeContent = await _azureStorageReadByteRangeOperations.GetOrDownloadContentAsync(blobUri, desiredOffset).ConfigureAwait(false);
                if (cachedHttpRangeContent == null)
                {
                    var message = $"The HTTP range obtained is invalid in {blobUri}, {desiredOffset}";
                    _logger.LogError(message);
                    throw new Exception(message);
                }

                byte[] mediaInfoBuffer;
                try
                {
                    // Copy to local buffer.
                    mediaInfoBuffer = cachedHttpRangeContent.CachedMemoryStream.ToArray();

                    // Tell MediaInfo what offset this represents in the file:
                    mediaInfoLib.OpenBufferInit(contentLength, cachedHttpRangeContent.CachedHttpRange.Offset);
                }
                catch (Exception e)
                {
                    var message = $"MediaInfoLib threw an unexpected exception on buffer initialization in {blobUri}";
                    _logger.LogError(message);
                    throw new Exception(message, e);
                }

                // Pin and send the buffer to MediaInfo, who will read and parse it:
                MediaInfoStatus result;
                try
                {
                    GCHandle gcHandle = GCHandle.Alloc(mediaInfoBuffer, GCHandleType.Pinned);
                    IntPtr addrOfBuffer = gcHandle.AddrOfPinnedObject();
                    result = (MediaInfoStatus)mediaInfoLib.OpenBufferContinue(addrOfBuffer, (IntPtr)mediaInfoBuffer.Length);
                    gcHandle.Free();
                }
                catch (Exception e) when (
                    e is ArgumentException ||
                    e is InvalidOperationException)
                {
                    var message = $"MediaInfoLib threw an unexpected exception on open buffer continuation in {blobUri}";
                    _logger.LogError(message);
                    throw new Exception(message, e);
                }

                // Check if MediaInfo is done.
                if ((result & MediaInfoStatus.Finalized) == MediaInfoStatus.Finalized)
                {
                    _logger.LogInformation($"MediaInfoFileReadFinalized in {blobUri}.");
                    break;
                }

                try
                {
                    // Test if MediaInfo requests to go elsewhere
                    desiredOffset = mediaInfoLib.OpenBufferContinueGoToGet();
                }
                catch (Exception e)
                {
                    var message = $"MediaInfoLib threw an unexpected exception on OpenBufferContinueGoToGet operation in {blobUri}, {desiredOffset}.";
                    _logger.LogError(message);
                    throw new Exception(message, e);
                }

                if (desiredOffset == contentLength)
                {
                    // MediaInfo requested EndOfFile:
                    _logger.LogInformation($"MediaInfoRequestedEndOfFile in {blobUri}, {desiredOffset}.");
                    break;
                }
                else if (desiredOffset == MediaInfoReadForward)
                {
                    // MediaInfo wants to continue reading forward
                    // Adjust the byte-range request offset
                    desiredOffset = (long)(cachedHttpRangeContent.CachedHttpRange.Offset + cachedHttpRangeContent.CachedHttpRange.Length);
                    _logger.LogInformation($"MediaInfoReadNewRangeRequested in {blobUri}, {desiredOffset}.");
                }
                else
                {
                    // Specific seek requested
                    _logger.LogInformation($"MediaInfoSeekRequested in {blobUri}, {desiredOffset}.");
                }

                if (desiredOffset >= contentLength)
                {
                    _logger.LogInformation($"MediaInfoMismatchInDesiredOffset in {blobUri}, {desiredOffset}.");
                    break;
                }
            }
            while (true);

            // This is the end of the stream, MediaInfo must finish some work
            mediaInfoLib.OpenBufferFinalize();

            // Use MediaInfoLib as needed
            mediaInfoLib.GetOption(CompleteOptionString, CompleteOptionValueString);
            mediaInfoLib.GetOption(OutputOptionString, JsonOutputValueString);
            var report = mediaInfoLib.GetInform();

            if (string.IsNullOrEmpty(report))
            {
                var message = $"InvalidMediaInfoLibReport for {blobUri}.";
                _logger.LogError(message);
                throw new Exception(message);
            }

            return JObject.Parse(report);
        }
    }
}