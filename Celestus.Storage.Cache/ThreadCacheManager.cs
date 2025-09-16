
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
                using var _ = from.ThreadLock();

                return to.TrySetCache(from.Cache.ToCache(), timeout ?? TimeSpan.FromMilliseconds(DEFAULT_TIMEOUT_IN_MS));
            }
            #endregion
        }
    }
}