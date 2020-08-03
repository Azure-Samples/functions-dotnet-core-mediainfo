using System;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;

namespace Storage.Helper
{
    /// <summary>
    /// Provides access to storage operations.
    /// </summary>
    public interface IAzureStorageOperations
    {
        /// <summary>
        /// Copies a Blob.
        /// </summary>
        /// <param name="sourceUri">The blobUri to read.</param>
        /// <param name="destinationUri">The target Uri.</param>
        /// <returns>The details and Content returned from copying a blob.</returns>
        Task<CopyFromUriOperation> BlobCopyAsync(Uri sourceUri, Uri destinationUri);

        /// <summary>
        /// Uploads a Blob.
        /// </summary>
        /// <param name="filePath">The file to upload.</param>
        /// <param name="destinationUri">The target Uri.</param>
        /// <returns>The details and Content returned from uploading a blob.</returns>
        Task<Response<BlobContentInfo>> BlobUploadAsync(string filePath, Uri destinationUri);

        /// <summary>
        /// Get a Read SAS for the blobUri that will expire.
        /// </summary>
        /// <param name="blobUri">The blobUri.</param>
        /// <param name="ttl">The Time to Live on the SAS.</param>
        /// <returns>Full SAS to the blobUri.</returns>
        Task<string> GetSasUrlAsync(Uri blobUri, TimeSpan ttl);

        /// <summary>
        /// Create or Get a container.
        /// </summary>
        /// <param name="containerUri">The containerUri.</param>
        /// <returns>BlobContainerInfo.</returns>
        Task<Response<BlobContainerInfo>> ContainerCreateIfNotExistsAsync(Uri containerUri);

        /// <summary>
        /// Checks if blob exists.
        /// </summary>
        /// <param name="blobUri">The blobUri.</param>
        /// <returns>Returns true if the blob exists.</returns>
        Task<bool> BlobExistsAsync(Uri blobUri);

        /// <summary>
        /// Gets the content length of the blob.
        /// </summary>
        /// <param name="blobUri">The blobUri.</param>
        /// <returns>The content-length in bytes.</returns>
        Task<long> GetBlobContentLengthAsync(Uri blobUri);
    }
}