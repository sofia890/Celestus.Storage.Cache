
namespace Celestus.Storage.Cache
{
    public class CacheFactoryCleaner<KeyType>
        where KeyType : class
    {
        readonly WeakReference<Dictionary<string, WeakReference<KeyType>>> _cachesReferences;
        readonly WeakReference<Dictionary<string, CacheCleanerBase<string>>> _cleanerReferences;

        long cleanupIntervalInTicks = TimeSpan.FromSeconds(10).Ticks;
        readonly Timer _timer;

        public CacheFactoryCleaner(Dictionary<string, WeakReference<KeyType>> caches, Dictionary<string, CacheCleanerBase<string>> cleaners)
        {
            _cachesReferences = new(caches);
            _cleanerReferences = new(cleaners);

            _timer = new Timer(Cleanup, null, cleanupIntervalInTicks, cleanupIntervalInTicks);
        }

        public void Cleanup(object? state)
        {
            if (_cachesReferences.TryGetTarget(out var _caches) &&
                _cleanerReferences.TryGetTarget(out var cleaners))
            {
                var deadKeys = _caches.Where(entry => !entry.Value.TryGetTarget(out var _))
                                      .Select(entry => entry.Key)
                                      .ToList();

                foreach (var deadKey in deadKeys)
                {
                    _ = _caches.Remove(deadKey);

                    cleaners[deadKey].Dispose();
                    cleaners.Remove(deadKey);
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
