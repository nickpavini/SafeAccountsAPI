using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SafeAccountsAPI.Data;
using Serilog;

namespace SafeAccountsAPI
{
    public class Program
    {

        public static IConfiguration Configuration { get; } =
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())

            .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}", optional: true, reloadOnChange: true)
            .Build();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <remarks></remarks>
        public static void Main(string[] args)
        {

            var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(Configuration)
            .CreateLogger();

            try
            {
                logger.Information("Starting web host");
                var host = CreateHostBuilder(args).Build();
                CreateDbIfNotExists(host);
                host.Run();
            }
            catch (System.Exception ex)
            {
                logger.Fatal(ex, "Host terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }


        }

        private static void CreateDbIfNotExists(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<APIContext>();
                    var config = services.GetRequiredService<IConfiguration>();
                    DbInitializer.Initialize(context, config);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred creating the DB.");
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                }).UseSerilog((hostingContext, loggerConfiguration) =>
                loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));
    }
}
