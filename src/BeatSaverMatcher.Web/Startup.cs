using BeatSaverMatcher.Common;
using BeatSaverMatcher.Common.BeatSaver;
using BeatSaverMatcher.Common.Db;
using BeatSaverMatcher.Web.Result;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace BeatSaverMatcher.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<SpotifyConfiguration>(Configuration.GetSection("Spotify"));
            services.AddTransient<SpotifyRepository>();
            services.AddTransient<MatchingService>();
            services.AddTransient<BeatSaverSongService>();
            services.AddTransient<BeatSaverRepository>();
            services.AddSingleton<WorkItemStore>();
            services.AddHostedService<SongMatchWorker>();
            services.AddHostedService<MatchCleanupWorker>();
            services.AddHostedService<MetricsServer>();
            services.AddHttpClient();

            var hasRedisConnection = !string.IsNullOrEmpty(Configuration["RedisConnection"]);

            if (hasRedisConnection)
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = Configuration["RedisConnection"];
                    options.InstanceName = "BeatSaverMatcher";
                });
            }
            else
            {
                services.AddDistributedMemoryCache();
            }

            services.Configure<DbConfiguration>(Configuration);
            services.AddTransient<IBeatSaberSongRepository, BeatSaberSongRepository>();

            services.AddResponseCompression();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseHttpMetrics();

            app.UseDefaultFiles();

            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
