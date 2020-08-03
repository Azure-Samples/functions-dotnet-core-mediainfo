using System;
using Microsoft.Extensions.Logging;
using MI = MediaInfo;

namespace Sample.MediaInfo.Core.Services
{
    /// <summary>
    /// Creates MediaInfo object.
    /// </summary>
    public class MediaInfoProvider : IMediaInfoProvider
    {
        private readonly ILogger<MediaInfoProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoProvider"/> class.
        /// </summary>
        /// <param name="logger">logger.</param>
        public MediaInfoProvider(ILogger<MediaInfoProvider> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public IMediaInfoService GetMediaInfoLib()
        {
            IMediaInfoService mediaInfoServiceLib;
            string libraryVersion;
            try
            {
                mediaInfoServiceLib = new MediaInfoServiceWrapper(new MI.MediaInfo());
                libraryVersion = mediaInfoServiceLib.GetOption("Info_Version", "0.7.0.0;MediaInfoDLL_Example_CS;0.7.0.0");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not load MediaInfo.dll, {e.Message}");
                throw;
            }

            if (string.IsNullOrEmpty(libraryVersion) || libraryVersion == "Unable to load MediaInfo library")
            {
                throw new InvalidOperationException($"Unable to load MediaInfo library, {libraryVersion}.");
            }

            return mediaInfoServiceLib;
        }
    }
}