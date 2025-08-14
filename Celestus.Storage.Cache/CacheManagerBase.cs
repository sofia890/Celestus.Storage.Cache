using System.Collections.Concurrent;

namespace Celestus.Storage.Cache
{
    public abstract class CacheManagerBase<CacheKeyType, CacheType>
        where CacheKeyType : class
        where CacheType : CacheBase<CacheKeyType>
    {
        // Track items that need to be disposed. This is needed due to code generator
        // not being able to implement dispose pattern correctly. Could not impose
        // pattern in a clean and user friendly way.
        readonly protected ConcurrentQueue<FactoryEntry<CacheKeyType, CacheType>> resources = [];

        readonly protected ConcurrentDictionary<string, WeakReference<CacheType>> _caches = [];
        readonly protected CacheManagerCleaner<string, CacheKeyType, CacheType> _factoryCleaner;

        public CacheManagerBase()
        {
            _factoryCleaner = new(new(resources));
        }

        public bool TryLoad(string key, out CacheType? cache)
        {
            cache = default;

            return _caches.TryGetValue(key, out var cacheReference) &&
                    cacheReference.TryGetTarget(out cache);
        }

        public CacheType GetOrCreateShared(string key = "")
        {
            var usedKey = (key.Length > 0) ? key : Guid.NewGuid().ToString();

            if (_caches.TryGetValue(usedKey, out var cacheReference) &&
                cacheReference.TryGetTarget(out var cache))
            {
                return cache;
            }
            else
            {
                var createdCache = (CacheType)Activator.CreateInstance(typeof(CacheType), [usedKey])!;
                _caches[usedKey] = new(createdCache);

                resources.Enqueue(new(new(createdCache), createdCache.Cleaner));

                return createdCache;
            }
        }

        public CacheType? UpdateOrLoadSharedFromFile(Uri path, TimeSpan? timeout = null)
        {
            if (TryCreateFromFile(path) is not CacheType loadedCache)
            {
                return null;
            }
            else if(TryLoad(loadedCache.Key, out var cacheToUpdate) && cacheToUpdate != null)
            {
                using (loadedCache)
                {
                    if (Update(loadedCache, cacheToUpdate, timeout))
                    {
                        return cacheToUpdate;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                _caches[loadedCache.Key] = new(loadedCache);

                resources.Enqueue(new(new(loadedCache), loadedCache.Cleaner));

                return loadedCache;
            }
        }

        public void SetCleanupInterval(TimeSpan interval)
        {
            _factoryCleaner.SetCleanupInterval(interval);
        }

        protected abstract CacheType? TryCreateFromFile(Uri path);

        protected abstract bool Update(CacheType from, CacheType to, TimeSpan? timeout);
    }
}
