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
