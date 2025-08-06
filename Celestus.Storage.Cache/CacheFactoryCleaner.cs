
namespace Celestus.Storage.Cache
{    
    public class CacheFactoryCleaner<KeyType>
        where KeyType : class
    {
        readonly WeakReference<Dictionary<string, WeakReference<KeyType>>> _cachesReference;
        long cleanupIntervalInTicks = TimeSpan.FromSeconds(10).Ticks;
        readonly Timer _timer;

        public CacheFactoryCleaner(Dictionary<string, WeakReference<KeyType>> caches)
        {
            _cachesReference = new(caches);

            _timer = new Timer(Cleanup, null, cleanupIntervalInTicks, cleanupIntervalInTicks);
        }

        public void Cleanup(object? state)
        {
            if (_cachesReference.TryGetTarget(out var _caches))
            {
                var deadKeys = _caches.Where(entry => !entry.Value.TryGetTarget(out var _))
                                      .Select(entry => entry.Key)
                                      .ToList();

                foreach (var deadKey in deadKeys)
                {
                    var _ = _caches.Remove(deadKey);
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
