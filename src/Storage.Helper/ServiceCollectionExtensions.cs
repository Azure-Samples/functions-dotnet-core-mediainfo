using System;
using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Storage.Helper.Services;

namespace Storage.Helper
{
    /// <summary>
    /// Extends <see cref="IServiceCollection"/> to register the services in this project.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds AzureStorageManagement.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddAzureStorageManagement(this IServiceCollection services)
        {
            services.AddSingleton<IAzureStorageManagement, AzureStorageManagement>();
        }

        /// <summary>
        /// Adds AzureStorageOperations.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddAzureStorageOperations(this IServiceCollection services)
        {
            services.AddSingleton<IAzureStorageOperations, AzureStorageOperations>();
            services.AddSingleton<IAzureStorageReadByteRangeOperations, AzureStorageReadByteRangeOperations>();
        }

        /// <summary>
        /// Adds <see cref="TokenCredential"/> for use in token-auth.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddAddAzureStorageDefaultAzureTokenCredential(this IServiceCollection services)
        {
            // https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            // Using DefaultAzureCredential allows for local dev by setting environment variables for the current user, provided said user
            // has the necessary credentials to perform the operations the MSI of the Function app needs in order to do its work. Including
            // interactive credentials will allow browser-based login when developing locally.
            services.AddSingleton<TokenCredential>(sp => new Azure.Identity.DefaultAzureCredential(includeInteractiveCredentials: true));
        }

        /// <summary>
        /// Adds <see cref="IAzure"/> for use in management operations.
        /// </summary>
        /// <param name="services">IServiceCollection object.</param>
        public static void AddAddAzureStorageAzureFluentManagement(this IServiceCollection services)
        {
            services.AddSingleton(sp =>
            {
                // If we find tenant and subscription in environment variables, configure accordingly
                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(@"AZURE_TENANT_ID"))
                    && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(@"AZURE_SUBSCRIPTION_ID")))
                {
                    var tokenCred = sp.GetService<TokenCredential>();
                    var armToken = tokenCred.GetToken(new TokenRequestContext(scopes: new[] { "https://management.azure.com/.default" }, parentRequestId: null), default).Token;
                    var armCreds = new Microsoft.Rest.TokenCredentials(armToken);

                    var graphToken = tokenCred.GetToken(new TokenRequestContext(scopes: new[] { "https://graph.windows.net/.default" }, parentRequestId: null), default).Token;
                    var graphCreds = new Microsoft.Rest.TokenCredentials(graphToken);

                    var credentials = new AzureCredentials(armCreds, graphCreds, Environment.GetEnvironmentVariable(@"AZURE_TENANT_ID"), AzureEnvironment.AzureGlobalCloud);

                    return Microsoft.Azure.Management.Fluent.Azure
                        .Authenticate(credentials)
                        .WithSubscription(Environment.GetEnvironmentVariable(@"AZURE_SUBSCRIPTION_ID"));
                }
                else
                {
                    var credentials = SdkContext.AzureCredentialsFactory
                        .FromSystemAssignedManagedServiceIdentity(MSIResourceType.AppService, AzureEnvironment.AzureGlobalCloud);
                    return Microsoft.Azure.Management.Fluent.Azure
                    .Authenticate(credentials)
                    .WithDefaultSubscription();
                }
            });
        }
    }
}
