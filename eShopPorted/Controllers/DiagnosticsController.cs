using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace eShopPorted.Controllers
{
    public class DiagnosticsController : Controller
    {
        private readonly IConfiguration configuration;

        public DiagnosticsController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        // GET: Diagnostics/KeyVault
        public ActionResult KeyVault()
        {
            ViewBag.KeyVaultUri = configuration["KeyVaultUri"];
            ViewBag.RandomSecret = configuration["demo-random-key"];
            return View();
        }
    }
}
