using System;
using System.Collections.Concurrent;
using System.Linq;

namespace BeatSaverMatcher.Web.Result
{
    public class WorkItemStore
    {
        private static readonly TimeSpan _resultKeepTime = TimeSpan.FromHours(1);

        private readonly ConcurrentQueue<WorkResultItem> _pendingItems;
        private readonly ConcurrentDictionary<string, WorkResultItem> _items;

        public WorkItemStore()
        {
            _pendingItems = new ConcurrentQueue<WorkResultItem>();
            _items = new ConcurrentDictionary<string, WorkResultItem>();
            //TODO: items cleanup!
        }

        public bool TryDequeue(out WorkResultItem item)
        {
            return _pendingItems.TryDequeue(out item);
        }

        public bool Enqueue(WorkResultItem item)
        {
            _items.AddOrUpdate(item.PlaylistId, key => item, (key, oldItem) => item);
            _pendingItems.Enqueue(item);
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
