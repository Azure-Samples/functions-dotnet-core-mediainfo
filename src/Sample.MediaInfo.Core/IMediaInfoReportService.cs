using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Sample.MediaInfo.Core
{
    /// <summary>
    /// Generate MediaInfo reports.
    /// </summary>
    public interface IMediaInfoReportService
    {
        /// <summary>
        /// Generate a MediaInfo "Inform" report with "Complete" option set, in JSON format.
        /// </summary>
        /// <param name="blobUri">The blobUri to analyze.</param>
        /// <returns>MediaInfo report.</returns>
        Task<JObject> GetMediaInfoCompleteInformForUriAsync(Uri blobUri);
    }
}