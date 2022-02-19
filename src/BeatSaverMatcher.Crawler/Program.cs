using BeatSaverMatcher.Common;
using BeatSaverMatcher.Common.BeatSaver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Crawler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(ConfigureLogging)
                .RunConsoleAsync();
        }

        private static void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(hostContext.Configuration)
                .CreateLogger();
            loggingBuilder.AddSerilog(Log.Logger);
        }

        private static void ConfigureAppConfiguration(HostBuilderContext ctx, IConfigurationBuilder configBuilder)
        {
            configBuilder
                .AddJsonFile("./config/appSettings.json", optional: true)
                .AddJsonFile("./config/logging.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        private static void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
        {
            services.Configure<DbConfiguration>(ctx.Configuration);
            services.AddTransient<BeatSaverRepository>();
            services.AddTransient<IBeatSaberSongRepository, BeatSaberSongRepository>();
            services.AddHostedService<CrawlerHost>();
            services.AddHostedService<MetricsServer>();
        }
    }
}
