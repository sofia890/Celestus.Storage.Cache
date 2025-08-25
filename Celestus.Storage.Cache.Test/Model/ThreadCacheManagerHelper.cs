namespace Celestus.Storage.Cache.Test.Model
{
    /// <summary>
    /// Helper class that extends ThreadCacheManager to expose the internal lock for testing
    /// </summary>
    internal class ThreadCacheManagerHelper : ThreadCache.ThreadCacheManager, IDoWhileLocked
    {
        public ThreadCacheManagerHelper(TimeSpan lockTimeout)
        {
            SetLockTimeoutInterval(lockTimeout);
        }

        public void CallCacheExpired(string key)
        {
            CacheExpired(key);
        }

        #region IDoWhileLocked
        public ReaderWriterLockSlim GetLock()
        {
            return _lock;
        }
        #endregion
    }
}
