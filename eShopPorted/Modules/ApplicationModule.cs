using System;
using Autofac;
using Azure.Identity;
using Azure.Storage.Blobs;
using eShopPorted.Models;
using eShopPorted.Models.Infrastructure;
using eShopPorted.Services;
using log4net;
using Microsoft.Extensions.Configuration;

namespace eShopPorted.Modules
{
    public class ApplicationModule : Module
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private bool _useMockData;
        private readonly IConfiguration _configuration;

        public ApplicationModule(bool useMockData, IConfiguration configuration)
        {
            _useMockData = useMockData;
            _configuration = configuration;
        }
        protected override void Load(ContainerBuilder builder)
        {
            if (_useMockData)
            {
                builder.RegisterType<CatalogServiceMock>()
                    .As<ICatalogService>()
                    .SingleInstance();
            }
            else
            {
                builder.RegisterType<CatalogService>()
                    .As<ICatalogService>()
                    .InstancePerLifetimeScope();
            }

            RegisterBlobStorage(builder);
        }

        private void RegisterBlobStorage(ContainerBuilder builder)
        {
            var serviceUri = _configuration?["BlobStorage:ServiceUri"];
            var picsContainer = _configuration?["BlobStorage:PicsContainer"];
            var documentsContainer = _configuration?["BlobStorage:DocumentsContainer"];

            if (!string.IsNullOrWhiteSpace(serviceUri))
            {
                // BlobServiceClient is thread-safe and must be a singleton (KB rule 26).
                builder.Register(_ =>
                {
                    var credentialOptions = new DefaultAzureCredentialOptions();

                    // AZURE_CLIENT_ID (App Service app setting) selects the user-assigned managed identity.
                    var managedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
                    if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
                    {
                        credentialOptions.ManagedIdentityClientId = managedIdentityClientId;
                    }

                    return new BlobServiceClient(new Uri(serviceUri), new DefaultAzureCredential(credentialOptions));
                })
                .As<BlobServiceClient>()
                .SingleInstance();
            }
            else
            {
                _log.Warn("BlobStorage:ServiceUri is not configured; Azure Blob Storage is disabled. " +
                          "The application will start, but blob-backed images/documents will be unavailable.");
            }

            builder.Register(c => new AzureBlobStorageService(
                    c.ResolveOptional<BlobServiceClient>(),
                    picsContainer,
                    documentsContainer))
                .As<IBlobStorageService>()
                .SingleInstance();
        }
    }
}