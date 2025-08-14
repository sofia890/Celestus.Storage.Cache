
using System.Collections.Concurrent;

namespace Celestus.Storage.Cache
{
    public record FactoryEntry<CacheKeyType, CacheType>(WeakReference<CacheType> Cache, CacheCleanerBase<CacheKeyType> Cleaner)
        where CacheKeyType : class
        where CacheType : CacheBase<CacheKeyType>;

    public class CacheManagerCleaner<ManagerKeyType, CacheKeyType, CacheType>
        where ManagerKeyType : class
        where CacheKeyType : class
        where CacheType : CacheBase<CacheKeyType>
    {
        public const int DEFAULT_INTERVAL_IN_MS = 10000;

        WeakReference<ConcurrentQueue<FactoryEntry<CacheKeyType, CacheType>>> _elements;

        long cleanupIntervalInTicks = TimeSpan.FromMilliseconds(DEFAULT_INTERVAL_IN_MS).Ticks;
        readonly Timer _timer;

        public CacheManagerCleaner(WeakReference<ConcurrentQueue<FactoryEntry<CacheKeyType, CacheType>>> elements)
        {
            _elements = elements;

            _timer = new Timer(Cleanup, null, cleanupIntervalInTicks, cleanupIntervalInTicks);
        }

        public void Cleanup(object? state)
        {
            if (_elements.TryGetTarget(out var elements))
            {
                var remainingElements = new List<FactoryEntry<CacheKeyType, CacheType>>();

                while (elements.TryDequeue(out var entry))
                {
                    if (!entry.Cache.TryGetTarget(out var abc))
                    {
                        entry.Cleaner.Dispose();
                    }
                    else if (abc.IsDisposed)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        remainingElements.Add(entry);
                    }
                }

                foreach (var entry in remainingElements)
                {
                    elements.Enqueue(entry);
                }
            }
            else
            {
                _timer.Dispose();
            }
        }

        public void SetCleanupInterval(TimeSpan interval)
        {
            cleanupIntervalInTicks = interval.Ticks;

            _timer.Change(cleanupIntervalInTicks, cleanupIntervalInTicks);
        }
    }
}
