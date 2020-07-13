using BeatSaverMatcher.Web.Result;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public class SongMatchWorker : IHostedService
    {
        private readonly MatchingService _matchingService;
        private readonly WorkItemStore _itemStore;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public SongMatchWorker(MatchingService matchingService, WorkItemStore itemStore)
        {
            _matchingService = matchingService;
            _itemStore = itemStore;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => DoWork());
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
                if(_itemStore.TryDequeue(out var item))
                {
                    await _matchingService.GetMatches(item);
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }
    }
}
