namespace Celestus.Storage.Cache
{
    public partial class ThreadCache
    {
        public static ThreadCacheManager Factory = new();

        public class ThreadCacheManager : CacheManagerBase<string, ThreadCache>
        {
            public void Remove(string key)
            {
                lock (this)
                {
                    if (_caches.TryGetValue(key, out var cacheReference))
                    {
                        _ = _caches.Remove(key, out _);
                    }
                }
            }

            #region CacheManagerBase
            protected override ThreadCache? TryCreateFromFile(Uri path)
            {
                return ThreadCache.TryCreateFromFile(path);
            }

            protected override bool Update(ThreadCache from, ThreadCache to, TimeSpan? timeout)
            {
                lock (this)
                {
                    var timeoutInMs = timeout?.Milliseconds ?? DEFAULT_TIMEOUT_IN_MS;
                    return to.TrySetCache(from.Cache.ToCache(), timeoutInMs);
                }
            }
            #endregion
        }
    }
}