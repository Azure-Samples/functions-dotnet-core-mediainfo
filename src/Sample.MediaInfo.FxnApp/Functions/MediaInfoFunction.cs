using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sample.MediaInfo.Core;

namespace Sample.Mediainfo.FxnApp.Functions
{
    /// <summary>
    /// Azure Function to get a MediaInfo report from a BlobUri.
    /// </summary>
    public class MediaInfoFunction
    {
        private readonly ILogger<MediaInfoFunction> _logger;
        private readonly IMediaInfoReportService _mediaInfoReportService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoFunction"/> class.
        /// </summary>
        /// <param name="logger">Injected <see cref="ILogger"/>.</param>
        /// <param name="mediaInfoReportService">njected <see cref="IMediaInfoReportService"/>.</param>
        public MediaInfoFunction(
            ILogger<MediaInfoFunction> logger,
            IMediaInfoReportService mediaInfoReportService)
        {
            _logger = logger;
            _mediaInfoReportService = mediaInfoReportService;
        }

        /// <summary>
        /// Gets the report from the <see cref="IMediaInfoReportService"/>.
        /// </summary>
        /// <param name="req">The POST request, with blobUri query or body.</param>
        /// <returns>The report in JSON format, or a error message.</returns>
        [FunctionName("MediaInfo")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            if (req?.Method == HttpMethods.Get)
            {
                var message = $"Do not use {req.Method}, use POST with a body or query of 'blobUri'.";
                _logger.LogInformation(message);
                return new BadRequestObjectResult(new { message });
            }

            string blobUriString = req?.Query["blobUri"];

            using (StreamReader streamReader = new StreamReader(req?.Body))
            {
                string requestBody = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                blobUriString ??= data?.blobUri;
            }

            if (!Uri.TryCreate(blobUriString, UriKind.Absolute, out Uri blobUri))
            {
                var message = $"Failed to parse 'blobUri' in query or body: {blobUriString}";
                _logger.LogInformation(message);
                return new BadRequestObjectResult(new { message });
            }

            JObject report;
            try
            {
                report = await _mediaInfoReportService.GetMediaInfoCompleteInformForUriAsync(blobUri).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to analyze {blobUri}");
                return new BadRequestObjectResult(new { message = $"Failed to analyze {blobUri}", exception = e });
            }

            return new OkObjectResult(report);
        }

        /*
        To test this function:
        curl -X POST \
            'http://localhost:7071/api/MediaInfo' \
            -H 'Content-Type: application/json' \
            -H 'cache-control: no-cache' \
            -d '
            {
                "blobUri": "https://youraccount.blob.core.windows.net/test/bbb.mp4"
            }
            ' -v
        Or:
        curl -X POST \
            'http://localhost:7071/api/MediaInfo?blobUri=https://youraccount.blob.core.windows.net/test/bbb.mp4' \
            -H 'Content-Type: application/json' -H 'cache-control: no-cache' -d '' -v
        */
    }
}
