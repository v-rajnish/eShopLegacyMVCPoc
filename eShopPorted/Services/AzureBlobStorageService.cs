using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using log4net;

namespace eShopPorted.Services
{
    /// <summary>
    /// Azure Blob Storage implementation of <see cref="IBlobStorageService"/> using a
    /// passwordless <c>BlobServiceClient</c> (DefaultAzureCredential). The client is
    /// injected as a singleton; when it is null the storage is treated as not configured.
    /// </summary>
    public class AzureBlobStorageService : IBlobStorageService
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _picsContainerName;
        private readonly string _documentsContainerName;

        public AzureBlobStorageService(BlobServiceClient blobServiceClient, string picsContainerName, string documentsContainerName)
        {
            _blobServiceClient = blobServiceClient;
            _picsContainerName = string.IsNullOrWhiteSpace(picsContainerName) ? "pics" : picsContainerName;
            _documentsContainerName = string.IsNullOrWhiteSpace(documentsContainerName) ? "documents" : documentsContainerName;
        }

        public bool IsConfigured => _blobServiceClient != null;

        public string PicsContainerName => _picsContainerName;

        public string DocumentsContainerName => _documentsContainerName;

        public async Task UploadAsync(string containerName, string blobName, Stream content, string contentType = null, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            var blobClient = containerClient.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions();
            if (!string.IsNullOrEmpty(contentType))
            {
                uploadOptions.HttpHeaders = new BlobHttpHeaders { ContentType = contentType };
            }

            // MIGRATION NOTE: unconditional overwrite (Conditions left null) to preserve the
            // last-write-wins semantics of the previous local-disk file write.
            await blobClient.UploadAsync(content, uploadOptions, cancellationToken).ConfigureAwait(false);
        }

        public async Task<byte[]> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            using (BlobDownloadStreamingResult download = response.Value)
            using (var memoryStream = new MemoryStream())
            {
                await download.Content.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                return memoryStream.ToArray();
            }
        }

        public async Task<IReadOnlyList<string>> ListAsync(string containerName, string prefix = null, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var names = new List<string>();

            if (!(await containerClient.ExistsAsync(cancellationToken).ConfigureAwait(false)).Value)
            {
                return names;
            }

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                names.Add(blobItem.Name);
            }

            return names;
        }

        public async Task<bool> DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var blobClient = _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);
            return (await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).Value;
        }

        public async Task<byte[]> GetPicAsync(string blobName, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                return null;
            }

            try
            {
                return await DownloadAsync(_picsContainerName, blobName, cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _log.Warn($"Pic blob '{blobName}' not found in container '{_picsContainerName}'.");
                return null;
            }
        }

        private void EnsureConfigured()
        {
            if (_blobServiceClient == null)
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage is not configured. Set the 'BlobStorage:ServiceUri' configuration key.");
            }
        }
    }
}
