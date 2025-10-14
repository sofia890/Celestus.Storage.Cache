using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    /// <summary>
    /// Single threaded cache cleaner.
    /// </summary>
    [JsonConverter(typeof(CacheCleanerJsonConverter))]
    public class CacheCleaner<CacheIdType, CacheKeyType>(TimeSpan interval) : CacheCleanerBase<CacheIdType, CacheKeyType>()
        where CacheIdType : notnull
        where CacheKeyType : notnull
    {
        const int DEFAULT_INTERVAL = 5000;

        TimeSpan _cleanupInterval = interval;
        DateTime _nextCleanupOpportunity;
        WeakReference<CacheBase<CacheIdType, CacheKeyType>>? _cacheReference;

        public CacheCleaner() : this(interval: TimeSpan.FromMilliseconds(DEFAULT_INTERVAL))
        {
            SetCleaningInterval(_cleanupInterval);
        }

        private void Prune(DateTime now)
        {
            if (_nextCleanupOpportunity > now)
            {
                return;
            }
            else if (_cacheReference == null ||
                     !_cacheReference.TryGetTarget(out var cache))
            {
                // Wait for reference to be available.
                return;
            }
            else
            {
                List<(CacheKeyType key, CacheEntry entry)> expiredKeys = [];

                foreach (var (key, entry) in cache.Storage)
                {
                    if (ExpiredCriteria(entry, now))
                    {
                        expiredKeys.Add((key, entry));
                    }
                }

                if (expiredKeys.Count > 0)
                {
                    _ = cache.TryRemove([.. expiredKeys.Select(x => x.key)]);
                }

                _nextCleanupOpportunity = now + _cleanupInterval;
            }
        }

        public static bool ExpiredCriteria(CacheEntry entry, DateTime now)
        {
            return entry.Expiration <= now;
        }

        public override void EntryAccessed(ref CacheEntry entry, CacheKeyType key)
        {
            EntryAccessed(ref entry, key, DateTime.UtcNow);
        }

        public override void EntryAccessed(ref CacheEntry entry, CacheKeyType key, DateTime when)
        {
            Prune(when);
        }

        public override void RegisterCache(WeakReference<CacheBase<CacheIdType, CacheKeyType>> cache)
        {
            _cacheReference = cache;
        }

        public override void UnregisterCache()
        {
            _cacheReference = null;
        }

        public override TimeSpan GetCleaningInterval()
        {
            return _cleanupInterval;
        }

        public override void SetCleaningInterval(TimeSpan interval)
        {
            _cleanupInterval = interval;
            _nextCleanupOpportunity = DateTime.UtcNow + _cleanupInterval;
        }

        public override object Clone()
        {
            return new CacheCleaner<CacheIdType, CacheKeyType>(TimeSpan.FromTicks(_cleanupInterval.Ticks));
        }
    }
}
