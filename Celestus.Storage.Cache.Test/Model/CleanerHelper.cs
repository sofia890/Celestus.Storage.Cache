namespace Celestus.Storage.Cache.Test.Model
{
    public static class CleanerHelper
    {
        public static void TrackNewEntry(CacheCleanerBase<string> cleaner, string key, DateTime expiration, object context, out CacheEntry entry)
        {
            entry = new(expiration.Ticks, null);

            cleaner.TrackEntry(ref entry, key);

            // ThreadCacheCleaner does not directly rely on TrackEntry for performance reasons.
            if (context is CacheCleanerThreadContext threadContext)
            {
                lock (threadContext)
                {
                    threadContext.Storage.Add(key, new(expiration.Ticks, new()));
                }
            }

        }

        public static void TrackNewEntry(CacheCleanerBase<string> cleaner, string key, DateTime expiration, object context)
        {
            TrackNewEntry(cleaner, key, expiration, context, out var _);
        }
    }
}
