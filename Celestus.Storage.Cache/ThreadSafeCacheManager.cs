namespace Celestus.Storage.Cache
{
    public partial class ThreadSafeCache
    {
        public static ThreadSafeCacheManager Factory { get; } = new();

        public class ThreadSafeCacheManager : CacheManagerBase<string, string, ThreadSafeCache>
        {
            public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromMilliseconds(5000);

            #region CacheManagerBase

            protected override ThreadSafeCache? TryCreateFromFile(FileInfo file)
            {
                return ThreadSafeCache.TryCreateFromFile(file);
            }

            protected override bool Update(ThreadSafeCache from, ThreadSafeCache to, TimeSpan? timeout)
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
