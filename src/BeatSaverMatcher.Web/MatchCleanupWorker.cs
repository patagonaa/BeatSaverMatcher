using BeatSaverMatcher.Web.Result;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class MatchCleanupWorker : BackgroundService
    {
        private readonly WorkItemStore _itemStore;
        private readonly ILogger<MatchCleanupWorker> _logger;

        public MatchCleanupWorker(WorkItemStore itemStore, ILogger<MatchCleanupWorker> logger)
        {
            _itemStore = itemStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();
                try
                {
                    _itemStore.DoCleanup();
                    _logger.LogDebug("Cleanup successful!");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while doing cleanup!");
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
