using System;
using System.IO;
using System.Threading.Tasks;
using log4net;

namespace eShopPorted.Services
{
    /// <summary>
    /// One-time startup seeder that uploads the bundled <c>wwwroot/Pics/*.png</c> assets
    /// into the "pics" blob container when the container is empty. No-ops when Azure Blob
    /// Storage is not configured so local development keeps working without blob.
    /// </summary>
    public static class BlobStorageSeeder
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BlobStorageSeeder));

        public static async Task SeedPicsAsync(IBlobStorageService blobStorage, string webRootPath)
        {
            if (blobStorage == null || !blobStorage.IsConfigured)
            {
                _log.Warn("BlobStorage:ServiceUri is not configured; skipping Pics blob seeding.");
                return;
            }

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                return;
            }

            var picsPath = Path.Combine(webRootPath, "Pics");
            if (!Directory.Exists(picsPath))
            {
                _log.Warn($"Local Pics folder '{picsPath}' does not exist; nothing to seed.");
                return;
            }

            try
            {
                // Only seed when the container is empty to keep this a one-time operation.
                var existing = await blobStorage.ListAsync(blobStorage.PicsContainerName).ConfigureAwait(false);
                if (existing.Count > 0)
                {
                    _log.Info($"Container '{blobStorage.PicsContainerName}' already contains {existing.Count} blob(s); skipping seeding.");
                    return;
                }

                var files = Directory.GetFiles(picsPath, "*.png");
                foreach (var file in files)
                {
                    var blobName = Path.GetFileName(file);
                    using (var stream = File.OpenRead(file))
                    {
                        await blobStorage.UploadAsync(blobStorage.PicsContainerName, blobName, stream, "image/png").ConfigureAwait(false);
                    }
                }

                _log.Info($"Seeded {files.Length} pic(s) into container '{blobStorage.PicsContainerName}'.");
            }
            catch (Exception ex)
            {
                // Seeding must never prevent the app from starting.
                _log.Error("Failed to seed Pics into Azure Blob Storage; continuing startup.", ex);
            }
        }
    }
}
