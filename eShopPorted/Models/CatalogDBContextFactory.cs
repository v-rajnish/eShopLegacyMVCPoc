using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace eShopPorted.Models
{
    // Enables `dotnet ef` design-time operations independent of the runtime UseMockData toggle.
    public class CatalogDBContextFactory : IDesignTimeDbContextFactory<CatalogDBContext>
    {
        public CatalogDBContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\mssqllocaldb;Database=eShopPorted;Trusted_Connection=True;MultipleActiveResultSets=true";

            var options = new DbContextOptionsBuilder<CatalogDBContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new CatalogDBContext(options);
        }
    }
}
