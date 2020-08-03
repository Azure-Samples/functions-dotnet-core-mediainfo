using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using RangeTree;
using Storage.Helper.Models;

namespace Storage.Helper.Services
{
    /// <summary>
    /// Provides access to storage operations.
    /// </summary>
    public sealed class AzureStorageReadByteRangeOperations : IAzureStorageReadByteRangeOperations, IDisposable
    {
        private const long MB = 1024 * 1024;

        /// <summary>
        /// Don't let the cache exceed a total of this many bytes.
        /// </summary>
        /// <remarks>
        /// Note: this is nearly true.  The current implementation will allow
        /// for a request to exceed this size (and succeed).  On the next
        /// subsequent request though, the overage will be noticed and the
        /// cache cleared before processing the request.
        /// </remarks>
        private const long MaxCachedBytes = 32 * MB;

        /// <summary>
        /// If the range reqeusted is less than this many bytes,
        /// round up to this many bytes for the Azure Storage Get request.
        /// </summary>
        private const long DefaultLength = 4 * MB;

        private readonly TokenCredential _tokenCredential;
        private readonly ILogger<AzureStorageOperations> _log;

        /// <summary>
        /// A cache of byte range data previously retrieved from Azure Storage for
        /// the blob named by LastUriCached.
        /// </summary>
        private readonly RangeTree<int, CachedHttpRangeContent> cachedContentTree = new RangeTree<int, CachedHttpRangeContent>();

        /// <summary>
        /// The URI for which cachedContentTree was holding entries.  When we change Uris
        /// cached content must be removed to avoid cross-contamination.
        /// </summary>
        private string lastUriCached = string.Empty;

        private long totalContentLength = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureStorageReadByteRangeOperations"/> class.
        /// </summary>
        /// <param name="tokenCredential">tokenCredential.</param>
        /// <param name="log">log.</param>
        public AzureStorageReadByteRangeOperations(
            TokenCredential tokenCredential,
            ILogger<AzureStorageOperations> log)
        {
            _tokenCredential = tokenCredential;
            _log = log;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            CleanUpCachedContentTree();
        }

        /// <inheritdoc/>
        public async Task<BlobDownloadInfo> DownloadHttpRangeAsync(Uri blobUri, HttpRange httpRange = default)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));
            var blobBaseClient = new BlobBaseClient(blobUri, _tokenCredential);

            // Note: when the httpRange struct is omitted it defaults to 'default' which downloads the entire blob.
            BlobDownloadInfo blobDownloadInfo;
            try
            {
                blobDownloadInfo = (await blobBaseClient.DownloadAsync(httpRange).ConfigureAwait(false)).Value;
            }
            catch (Exception e)
            {
                var message = $"Could not download the HTTP range for {blobUri}.";
                _log.LogError(message, e);
                throw new Exception(message, e);
            }

            return blobDownloadInfo;
        }

        /// <inheritdoc/>
        public async Task<CachedHttpRangeContent> GetOrDownloadContentAsync(Uri blobUri, long desiredOffset)
        {
            _ = blobUri ?? throw new ArgumentNullException(nameof(blobUri));

            if (desiredOffset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(desiredOffset), $"Must be greater than zero. {desiredOffset}");
            }

            // Since the cachedContentTree is only managed on a single-URI basis, we need
            // to determine if the cache content is for the requested URI.  If not, flush
            // out the cache as we're starting over for a new URI.
            string uriString = blobUri.ToString();

            if (uriString != lastUriCached)
            {
                CleanUpCachedContentTree();
                lastUriCached = uriString;
            }
            else
            {
                // It's for the same URI as last call, so check cache for ranges that contain this offset.
                var cachedHttpRangeContentEntry = cachedContentTree
                    .Query((int)desiredOffset)
                    .OrderByDescending(e => e.CachedHttpRange.Offset)
                    .FirstOrDefault();

                if (cachedHttpRangeContentEntry != default(CachedHttpRangeContent))
                {
                    var message = $"Found desiredOffset, {desiredOffset}, in existing range.";
                    _log.LogInformation(message);
                    return cachedHttpRangeContentEntry;
                }
            }

            // No luck, nothing suitable in the cache so we're going to have to download a new range to cover
            // the request. Clean out the whole tree if we are about to exceed the MaxMemorySize.
            if (totalContentLength + DefaultLength >= MaxCachedBytes)
            {
                CleanUpCachedContentTree();
            }

            int downloadedContentLength = 0;
            MemoryStream memStream = null;
            var requestedHttpRange = new HttpRange(desiredOffset, DefaultLength);
            try
            {
                using var downloadResponse = await DownloadHttpRangeAsync(blobUri, requestedHttpRange).ConfigureAwait(false);
                downloadedContentLength = (int)downloadResponse.ContentLength;
                totalContentLength += downloadedContentLength;

#pragma warning disable CA2000 // Dispose objects before losing scope
                memStream = new MemoryStream(downloadedContentLength);
#pragma warning restore CA2000 // Dispose objects before losing scope

                downloadResponse.Content.CopyTo(memStream);
            }
            catch (Exception e) when (
                e is ArgumentOutOfRangeException ||
                e is ArgumentNullException ||
                e is NotSupportedException ||
                e is ObjectDisposedException ||
                e is IOException)
            {
                var message = $"Could not download content for {blobUri}.";
                _log.LogError(message, e);
                throw new Exception(message, e);
            }

            var actualHttpRange = new HttpRange(desiredOffset, downloadedContentLength);
            var cachedHttpRangeContent = new CachedHttpRangeContent(actualHttpRange, memStream);
            cachedContentTree.Add((int)actualHttpRange.Offset, (int)(actualHttpRange.Offset + actualHttpRange.Length - 1), cachedHttpRangeContent);

            // Console.WriteLine($"Added Range:\t\t {actualHttpRange.Offset},\t\t {actualHttpRange.Offset + actualHttpRange.Length - 1}");
            _log.LogInformation($"HttpRangeDownloadedFinished, {actualHttpRange.Offset}, {actualHttpRange.Length}");

            return cachedHttpRangeContent;
        }

        /// <summary>
        /// Removes and disposes of the contents of the cachedContentTree.
        /// </summary>
        private void CleanUpCachedContentTree()
        {
            if (cachedContentTree.Any())
            {
                foreach (var rangeValuePair in cachedContentTree)
                {
                    cachedContentTree.Remove(rangeValuePair.Value);
                    rangeValuePair.Value.Dispose();
                }
            }

            totalContentLength = 0;
        }
    }
}
