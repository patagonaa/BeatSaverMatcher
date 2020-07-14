using BeatSaverMatcher.Web.Result;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class MatchCleanupWorker : IHostedService
    {
        private readonly WorkItemStore _itemStore;
        private readonly ILogger<MatchCleanupWorker> _logger;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public MatchCleanupWorker(WorkItemStore itemStore, ILogger<MatchCleanupWorker> logger)
        {
            _itemStore = itemStore;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() => DoWork(), TaskCreationOptions.LongRunning);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }

        private async Task DoWork()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    _itemStore.DoCleanup();
                    _logger.LogInformation("Cleanup successful!");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error while doing cleanup!");
                    throw;
                }

                await Task.Delay(TimeSpan.FromMinutes(10));
            }
        }
    }
}
