namespace Celestus.Storage.Cache.Test.Model
{
    /// <summary>
    /// Helper class that extends CacheManager to expose the internal lock for testing
    /// </summary>
    internal class CacheManagerHelper : Cache.CacheManager, IDoWhileLocked
    {
        public CacheManagerHelper(TimeSpan lockTimeout)
        {
            SetLockTimeoutInterval(lockTimeout);
        }

        public void CallCacheExpired(string key)
        {
            Remove(key);
        }

        public static void SetCleanupInterval(Type cacheType, TimeSpan interval)
        {
            if (typeof(Cache) == cacheType)
            {
                Cache.Factory.SetCleanupInterval(interval);
            }
            else
            {
                ThreadSafeCache.Factory.SetCleanupInterval(interval);
            }
        }

        public static void ResetCleanupInterval(Type cacheType)
        {
            if (typeof(Cache) == cacheType)
            {
                Cache.Factory.SetCleanupInterval(TimeSpan.FromMilliseconds(CacheManagerCleaner<string, string, Cache>.DEFAULT_INTERVAL_IN_MS));
            }
            else
            {
                ThreadSafeCache.Factory.SetCleanupInterval(TimeSpan.FromMilliseconds(CacheManagerCleaner<string, string, Cache>.DEFAULT_INTERVAL_IN_MS));
            }
        }

        #region IDoWhileLocked
        public ReaderWriterLockSlim GetLock()
        {
            return _lock;
        }
        #endregion
    }
}
