
namespace Celestus.Storage.Cache
{
    public partial class ThreadCache
    {
        public static ThreadCacheManager Factory { get; } = new();

        public class ThreadCacheManager : CacheManagerBase<string, ThreadCache>
        {

            #region CacheManagerBase

            protected override ThreadCache? TryCreateFromFile(Uri path)
            {
                return ThreadCache.TryCreateFromFile(path);
            }

            protected override bool Update(ThreadCache from, ThreadCache to, TimeSpan? timeout)
            {
                // Should really be a read lock and not a write lock but we have write lock easily
                // available.
                if (from.TryGetThreadWriteLock(out var cacheLock))
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