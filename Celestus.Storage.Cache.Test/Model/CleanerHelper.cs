namespace Celestus.Storage.Cache.Test.Model
{
    internal static class CleanerHelper
    {
        public static void TrackNewEntry(
            CacheCleanerBase<string> cleaner,
            string key, DateTime expiration,
            CacheCleanerContext context,
            out CacheEntry entry
        )
        {
            entry = new(expiration.Ticks, null);

            lock (context)
            {
                context.Storage.Add(key, new(expiration.Ticks, new()));
            }

            cleaner.TrackEntry(ref entry, key);
        }

        public static void TrackNewEntry(CacheCleanerBase<string> cleaner, string key, DateTime expiration, CacheCleanerContext context)
        {
            TrackNewEntry(cleaner, key, expiration, context, out var _);
        }
    }
}
