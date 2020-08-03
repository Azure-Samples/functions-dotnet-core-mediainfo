using Sample.MediaInfo.Core.Services;

namespace Sample.MediaInfo.Core
{
    /// <summary>
    /// MediaInfoProvider interface.
    /// </summary>
    public interface IMediaInfoProvider
    {
        /// <summary>
        /// Gets the MediaInfo library from the Nuget package.
        /// </summary>
        /// <returns><see cref="MediaInfoServiceWrapper"/>.</returns>
        IMediaInfoService GetMediaInfoLib();
    }
}