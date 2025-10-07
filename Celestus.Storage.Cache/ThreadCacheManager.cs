namespace Celestus.Storage.Cache
{
    public partial class ThreadCache
    {
        public static ThreadCacheManager Factory { get; } = new();

        public class ThreadCacheManager : CacheManagerBase<string, string, ThreadCache>
        {
            public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromMilliseconds(5000);

            #region CacheManagerBase

            protected override ThreadCache? TryCreateFromFile(FileInfo file)
            {
                return ThreadCache.TryCreateFromFile(file);
            }

            protected override bool Update(ThreadCache from, ThreadCache to, TimeSpan? timeout)
            {
                // Should really be a read lock and not a write lock but we have write lock easily
                // available.
                if (from.TryGetWriteLock(out var cacheLock))
                {
                    using (cacheLock)
                    {
                        return to.TrySetCache(from.Cache.ToCache(), timeout ?? DefaultTimeout);
                    }
                }
                else
                {
                    return false;
                }
            }
            #endregion
        }
    }
}