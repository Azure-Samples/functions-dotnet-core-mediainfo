using System;
using System.Linq;
using Microsoft.Azure.Management.Fluent;

namespace Storage.Helper.Services
{
    /// <summary>
    /// Provides access to storage management methods.
    /// </summary>
    public class AzureStorageManagement : IAzureStorageManagement
    {
        private readonly IAzure _azure;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureStorageManagement"/> class.
        /// </summary>
        /// <param name="azure">An IAzure interface.</param>
        public AzureStorageManagement(IAzure azure)
        {
            _azure = azure;
        }

        /// <inheritdoc/>
        public string GetAccountKey(string accountName)
        {
            var storageAccounts = _azure.StorageAccounts.List();
            var accountKeys = storageAccounts
                .FirstOrDefault(sa => sa.Name.Equals(accountName, StringComparison.OrdinalIgnoreCase))?
                .GetKeys();

            // Return primary key as it allows for key-rolling mechanism
            var accountKey = accountKeys?.FirstOrDefault(k => k.KeyName.Equals(@"key1", StringComparison.OrdinalIgnoreCase))?.Value;

            return accountKey;
        }
    }
}
