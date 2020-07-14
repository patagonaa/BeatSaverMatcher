using Prometheus;
using System;
using System.Collections.Concurrent;

namespace BeatSaverMatcher.Web.Result
{
    public class WorkItemStore
    {
        private static readonly TimeSpan _resultKeepTime = TimeSpan.FromHours(1);

        private readonly ConcurrentQueue<WorkResultItem> _pendingItems;
        private readonly ConcurrentDictionary<string, WorkResultItem> _items;
        private readonly Gauge _pendingRequestsGauge;

        public WorkItemStore()
        {
            _pendingItems = new ConcurrentQueue<WorkResultItem>();
            _items = new ConcurrentDictionary<string, WorkResultItem>();

            _pendingRequestsGauge = Metrics.CreateGauge("beatsaver_waiting_requests", "Requests waiting to be processed");
        }

        public bool TryDequeue(out WorkResultItem item)
        {
            bool dequeued = _pendingItems.TryDequeue(out item);
            if (dequeued)
                _pendingRequestsGauge.Dec();
            return dequeued;
        }

        public bool Enqueue(WorkResultItem item)
        {
            _items.AddOrUpdate(item.PlaylistId, key => item, (key, oldItem) => item);
            _pendingItems.Enqueue(item);
            _pendingRequestsGauge.Inc();
            return true;
        }

        public WorkResultItem Get(string playlistId)
        {
            if (_items.TryGetValue(playlistId, out var item))
                return item;
            return null;
        }

        public void DoCleanup()
        {
            var items = _items.Values;

            foreach (var item in items)
            {
                if (item.CreatedAt < (DateTime.UtcNow - _resultKeepTime))
                {
                    _items.TryRemove(item.PlaylistId, out _);
                }
            }
        }
    }
}
