namespace Celestus.Storage.Cache.Test.Model
{
    /// <summary>
    /// Helper class that extends ThreadSafeCacheManager to expose the internal lock for testing
    /// </summary>
    internal class ThreadSafeCacheManagerHelper : ThreadSafeCache.ThreadSafeCacheManager, IDoWhileLocked
    {
        public ThreadSafeCacheManagerHelper(TimeSpan lockTimeout)
        {
            SetLockTimeoutInterval(lockTimeout);
        }

        public void CallCacheExpired(string key)
        {
            Remove(key);
        }

        #region IDoWhileLocked
        public ReaderWriterLockSlim GetLock()
        {
            return _lock;
        }
        #endregion
    }
}
