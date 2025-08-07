namespace Celestus.Storage.Cache
{
    public class CacheManager
    {
        // Track items that need to be disposed. This is needed due to code generator
        // not being able to implement dispose pattern correctly. Could not impose
        // pattern in a clean and user friendly way.
        readonly static Dictionary<string, CacheCleanerBase<string>> _cleaners = [];

        readonly static Dictionary<string, WeakReference<Cache>> _caches = [];
        readonly static CacheFactoryCleaner<Cache> _factoryCleaner = new(_caches, new());

        public static bool IsLoaded(string key, out Cache? cache)
        {
            cache = null;

            return _caches.TryGetValue(key, out var cacheReference) &&
                   cacheReference.TryGetTarget(out cache);
        }

        public static Cache GetOrCreateShared(string key = "")
        {
            var usedKey = (key.Length > 0) ? key : Guid.NewGuid().ToString();

            lock (nameof(Cache))
            {
                if (_caches.TryGetValue(usedKey, out var cacheReference) &&
                    cacheReference.TryGetTarget(out var cache))
                {
                    return cache;
                }
                else
                {
                    cache = new Cache(usedKey);

                    _caches[usedKey] = new(cache);

                    if (cache.Cleaner != null)
                    {
                        _cleaners[usedKey] = cache.Cleaner;
                    }

                    return cache;
                }
            }
        }

        public static Cache? UpdateOrLoadSharedFromFile(Uri path)
        {
            if (Cache.TryCreateFromFile(path) is not Cache loadedCache)
            {
                return null;
            }
            else if (IsLoaded(loadedCache.Key, out var cache) &&
                     cache != null)
            {
                lock (nameof(Cache))
                {
                    var cacheReference = _caches[loadedCache.Key];

                    cache.Storage = loadedCache.Storage;

                    return cache;
                }
            }
            else
            {
                lock (nameof(Cache))
                {
                    _caches[loadedCache.Key] = new(loadedCache);
                }

                return loadedCache;
            }
        }
    }
}
