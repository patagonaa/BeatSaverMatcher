using BeatSaverMatcher.Web.Result;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaverMatcher.Web
{
    public sealed class SongMatchWorker : BackgroundService, IDisposable
    {
        private const int _maxRunningTasks = 8;
        private readonly ILogger<SongMatchWorker> _logger;
        private readonly MatchingService _matchingService;
        private readonly WorkItemStore _itemStore;
        private readonly Gauge _runningMatchesGauge;
        private readonly List<Task> _runningTasks = new List<Task>();

        public SongMatchWorker(ILogger<SongMatchWorker> logger, MatchingService matchingService, WorkItemStore itemStore)
        {
            _logger = logger;
            _matchingService = matchingService;
            _itemStore = itemStore;
            _runningMatchesGauge = Metrics.CreateGauge("beatsaver_running_requests", "Requests currently running");
        }

        protected override async Task ExecuteAsync(CancellationToken cancelToken)
        {
            while (true)
            {
                try
                {
                    cancelToken.ThrowIfCancellationRequested();
                    _runningMatchesGauge.Set(_runningTasks.Count(x => x.Status == TaskStatus.Running || x.Status == TaskStatus.WaitingForActivation || x.Status == TaskStatus.WaitingForChildrenToComplete));
                    if (_runningTasks.Count > _maxRunningTasks)
                    {
                        var task = await Task.WhenAny(_runningTasks);
                        _runningTasks.Remove(task);
                    }
                    else if (_itemStore.TryDequeue(out var item))
                    {
                        var task = Task.Run(() => _matchingService.GetMatches(item, cancelToken), cancelToken);
                        _runningTasks.Add(task);
                    }
                    else
                    {
                        await Task.Delay(1000, cancelToken);
                    }
                }
                catch (OperationCanceledException) when (cancelToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while managing matching");
                }
            }
        }
    }
}
