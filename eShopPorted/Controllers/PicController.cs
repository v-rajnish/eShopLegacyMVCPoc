using eShopPorted.Services;
using log4net;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace eShopPorted.Controllers
{
    public class PicController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string GetPicRouteName = "GetPicRouteTemplate";

        private readonly ICatalogService service;
        private readonly IBlobStorageService blobStorage;

        public PicController(ICatalogService service, IBlobStorageService blobStorage)
        {
            this.service = service;
            this.blobStorage = blobStorage;
        }

        // GET: items/5/pic
        [HttpGet]
        [Route("items/{catalogItemId:int}/pic", Name = GetPicRouteName)]
        public async Task<IActionResult> Index(int catalogItemId)
        {
            _log.Info($"Now loading... /items/Index?{catalogItemId}/pic");

            if (catalogItemId <= 0)
            {
                return BadRequest();
            }

            var item = service.FindCatalogItem(catalogItemId);

            if (item != null)
            {
                // Guard against path traversal by using only the file name portion.
                var fileName = Path.GetFileName(item.PictureFileName);
                return await GetPicResultAsync(fileName);
            }

            return NotFound();
        }

        // GET: /Pics/1.png — preserves the PictureUri = /Pics/{Id}.png contract by serving
        // the image from the "pics" blob container through the controller.
        [HttpGet]
        [Route("Pics/{fileName}")]
        public Task<IActionResult> Get(string fileName)
        {
            _log.Info($"Now loading... /Pics/{fileName}");
            return GetPicResultAsync(Path.GetFileName(fileName));
        }

        private async Task<IActionResult> GetPicResultAsync(string fileName)
        {
            var buffer = await blobStorage.GetPicAsync(fileName);

            if (buffer == null)
            {
                return NotFound();
            }

            string imageFileExtension = Path.GetExtension(fileName);
            string mimetype = GetImageMimeTypeFromImageFileExtension(imageFileExtension);

            return File(buffer, mimetype);
        }

        private string GetImageMimeTypeFromImageFileExtension(string extension)
        {
            string mimetype;

            switch (extension)
            {
                case ".png":
                    mimetype = "image/png";
                    break;
                case ".gif":
                    mimetype = "image/gif";
                    break;
                case ".jpg":
                case ".jpeg":
                    mimetype = "image/jpeg";
                    break;
                case ".bmp":
                    mimetype = "image/bmp";
                    break;
                case ".tiff":
                    mimetype = "image/tiff";
                    break;
                case ".wmf":
                    mimetype = "image/wmf";
                    break;
                case ".jp2":
                    mimetype = "image/jp2";
                    break;
                case ".svg":
                    mimetype = "image/svg+xml";
                    break;
                default:
                    mimetype = "application/octet-stream";
                    break;
            }

            return mimetype;
        }
    }
}
