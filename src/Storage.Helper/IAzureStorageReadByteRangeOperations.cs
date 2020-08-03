using System;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Storage.Helper.Models;

namespace Storage.Helper
{
    /// <summary>
    /// Provides access to storage operations.
    /// </summary>
    public interface IAzureStorageReadByteRangeOperations
    {
        /// <summary>
        /// Downloads a byte-range.
        /// Users should call BlobDownloadInfo.Dispose when done reading/copying the data.
        /// </summary>
        /// <param name="blobUri">The target Uri.</param>
        /// <param name="httpRange">Optional httpRange, if omitted 'default' is used to download all. Optimal is usually 4MB.</param>
        /// <returns>The Azure.Storage.Blobs.Models.BlobDownloadInfo containing meta and Content from downloading a byte range.</returns>
        Task<BlobDownloadInfo> DownloadHttpRangeAsync(Uri blobUri, HttpRange httpRange = default);

        /// <summary>
        /// Use this to read and seek within a large file.  This function downloads a byte-range
        /// (which is chunk-size-optimized for AzureStorage) to the client, and allows the caller
        /// to read that MemoryStream.
        /// Further calls will not dispose of the already downloaded data, until a memory threshold
        /// is exceeded. This will reduce network calls for seeks back into the data.
        /// NOTE: The desiredOffset MAY NOT be at the beginning of the MemoryStream.
        /// NOTE: The caller should not dispose of the memory stream.
        /// NOTE: This will cache-miss and be no better than range-requests for a sequence of
        /// backward seeks less than DownloadByteBufferSize.  Use DownloadHttpRangeAsync if this is your use case.
        /// </summary>
        /// <param name="blobUri">The blobUri to read.</param>
        /// <param name="desiredOffset">The desired offset that must be within the stream.</param>
        /// <returns>
        /// Provides a MemoryStream containing the desiredOffset, and an HttpRange which
        /// describes the actual starting offset and buffer length of the MemoryStream relative
        /// to the blobUri.
        /// </returns>
        Task<CachedHttpRangeContent> GetOrDownloadContentAsync(Uri blobUri, long desiredOffset);
    }
}