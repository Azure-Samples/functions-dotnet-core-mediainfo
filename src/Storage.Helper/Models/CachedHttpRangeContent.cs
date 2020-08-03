﻿using System;
using System.IO;
using Azure;

namespace Storage.Helper.Models
{
    /// <summary>
    /// Provides the range and buffer that has been cached.
    /// </summary>
    public sealed class CachedHttpRangeContent : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CachedHttpRangeContent"/> class.
        /// </summary>
        /// <param name="range">range.</param>
        /// <param name="memoryStream">memoryStream.</param>
        public CachedHttpRangeContent(HttpRange range, MemoryStream memoryStream)
        {
            CachedHttpRange = range;
            CachedMemoryStream = memoryStream;
        }

        /// <summary>
        /// Gets the actual Offset and Length of the memory stream, relative to the blobUri.
        /// </summary>
        public HttpRange CachedHttpRange { get; private set; }

        /// <summary>
        /// Gets the MemoryStream that has been cached.
        /// </summary>
        public MemoryStream CachedMemoryStream { get; private set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            CachedMemoryStream?.Dispose();
        }
    }
}
