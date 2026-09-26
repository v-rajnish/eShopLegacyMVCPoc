using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Azure.Identity;
using eShopPorted.Models;
using eShopPorted.Modules;
using eShopPorted.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eShopPorted
{
    public class Program
    {
        public static DateTime StartTime { get; } = DateTime.UtcNow;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // If KeyVaultUri is set (injected by Bicep in Azure, optional locally),
            // pull secrets directly into IConfiguration using the app's identity.
            // Locally, DefaultAzureCredential falls back to your `az login` session.
            // Wrapped in try/catch: Key Vault being unreachable (firewall, network,
            // permissions) must never prevent the app from starting.
            string keyVaultUri = builder.Configuration["KeyVaultUri"];
            bool keyVaultLoaded = false;
            if (!string.IsNullOrWhiteSpace(keyVaultUri))
            {
                try
                {
                    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
                    keyVaultLoaded = true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Startup] Key Vault unreachable at '{keyVaultUri}', continuing without it: {ex.Message}");
                }
            }

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            bool useMockData = builder.Configuration.GetValue<bool>("UseMockData");

            builder.Services.AddControllersWithViews();
            builder.Services.AddApplicationInsightsTelemetry();

            if (!useMockData)
            {
                string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                builder.Services.AddDbContext<CatalogDBContext>(options =>
                    options.UseSqlServer(connectionString));
            }

            builder.Host.ConfigureContainer<ContainerBuilder>(container =>
                container.RegisterModule(new ApplicationModule(useMockData, builder.Configuration)));

            var app = builder.Build();

            // Proves the SDK-based Key Vault read worked, without logging the secret value itself.
            if (keyVaultLoaded)
            {
                var demoSecret = app.Configuration["demo-api-key"];
                app.Logger.LogInformation(
                    "Key Vault demo secret 'demo-api-key' loaded: {Loaded} (length={Length})",
                    string.IsNullOrEmpty(demoSecret) ? "NO" : "YES",
                    demoSecret?.Length ?? 0);
            }

            if (!useMockData)
            {
                using var scope = app.Services.CreateScope();
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
                    db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    // Don't crash startup if the database isn't reachable/authorized yet.
                    var logger = app.Services.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("Startup");
                    logger.LogError(ex, "Database migration failed at startup; continuing without it.");
                }
            }

            // One-time seeding of the bundled Pics into the "pics" blob container.
            // No-ops when BlobStorage:ServiceUri is not configured (local dev without blob).
            using (var seedScope = app.Services.CreateScope())
            {
                try
                {
                    var blobStorage = seedScope.ServiceProvider.GetRequiredService<IBlobStorageService>();
                    var webHostEnv = seedScope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                    BlobStorageSeeder.SeedPicsAsync(blobStorage, webHostEnv.WebRootPath).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("Startup");
                    logger.LogError(ex, "Pics blob seeding failed at startup; continuing without it.");
                }
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Catalog}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
