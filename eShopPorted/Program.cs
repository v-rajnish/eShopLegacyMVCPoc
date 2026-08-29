using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using eShopPorted.Models;
using eShopPorted.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eShopPorted
{
    public class Program
    {
        public static DateTime StartTime { get; } = DateTime.UtcNow;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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
                container.RegisterModule(new ApplicationModule(useMockData)));

            var app = builder.Build();

            if (!useMockData)
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CatalogDBContext>();
                db.Database.Migrate();
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

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Catalog}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
