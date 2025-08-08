using System.Threading;

namespace Celestus.Storage.Cache
{
    public static class ThreadCacheManager
    {
        // Track items that need to be disposed. This is needed due to code generator
        // not being able to implement dispose pattern correctly. Could not impose
        // pattern in a clean and user friendly way.
        readonly static Dictionary<string, CacheCleanerBase<string>> _cleaners = [];

        readonly static Dictionary<string, WeakReference<ThreadCache>> _caches = [];
        readonly static CacheFactoryCleaner<ThreadCache> _factoryCleaner = new(_caches, _cleaners);

        readonly static object _lock = new(); 

        public static bool IsLoaded(string key)
        {
            return _caches.ContainsKey(key);
        }

        public static ThreadCache GetOrCreateShared(string key = "")
        {
            var usedKey = (key.Length > 0) ? key : Guid.NewGuid().ToString();

            lock (_lock)
            {
                if (_caches.TryGetValue(usedKey, out var cacheReference) &&
                    cacheReference.TryGetTarget(out var cache))
                {
                    return cache;
                }
                else
                {
                    cache = new ThreadCache(usedKey);

                    _caches[usedKey] = new(cache);

                    if (cache.Cleaner != null)
                    {
                        _cleaners[usedKey] = cache.Cleaner;
                    }

                    return cache;
                }
            }
        }

        public static ThreadCache? UpdateOrLoadSharedFromFile(Uri path, int timeout = ThreadCache.NO_TIMEOUT)
        {
            if (ThreadCache.TryCreateFromFile(path) is not ThreadCache loadedCache)
            {
                return null;
            }
            else if (IsLoaded(loadedCache.Key))
            {
                lock (_lock)
                {
                    var threadCacheReference = _caches[loadedCache.Key];

                    if (threadCacheReference.TryGetTarget(out var threadCache) &&
                        threadCache.TrySetCache(loadedCache.Cache, timeout))
                    {
                        return threadCache;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                lock (_lock)
                {
                    _caches[loadedCache.Key] = new(loadedCache);
                }

                return loadedCache;
            }
        }

        public static void Remove(string key)
        {
            lock (_lock)
            {
                if (_caches.TryGetValue(key, out var cacheReference))
                {
                    _caches.Remove(key);
                }
            }
        }

        public static void SetCleanupInterval(TimeSpan interval)
        {
            _factoryCleaner.SetCleanupInterval(interval);
        }
    }
}
