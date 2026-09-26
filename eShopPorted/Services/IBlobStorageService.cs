using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace eShopPorted.Services
{
    /// <summary>
    /// Abstraction over Azure Blob Storage for whole-file and document storage.
    /// Replaces the previous local-disk file access.
    /// </summary>
    public interface IBlobStorageService
    {
        // False when BlobStorage:ServiceUri is not configured, so callers can no-op locally.
        bool IsConfigured { get; }

        string PicsContainerName { get; }

        string DocumentsContainerName { get; }

        Task UploadAsync(string containerName, string blobName, Stream content, string contentType = null, CancellationToken cancellationToken = default);

        Task<byte[]> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> ListAsync(string containerName, string prefix = null, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

        // Returns the image bytes from the "pics" container, or null when the blob is missing / storage is not configured.
        Task<byte[]> GetPicAsync(string blobName, CancellationToken cancellationToken = default);
    }
}
