namespace Celestus.Storage.Cache.Test.Model
{
    public static class CleanerHelper
    {
        public static void TrackNewEntry(CacheCleanerBase<string> cleaner, string key, DateTime expiration, out CacheEntry entry)
        {
            entry = new(expiration.Ticks, null);

            cleaner.TrackEntry(ref entry, key);
        }

        public static void TrackNewEntry(CacheCleanerBase<string> cleaner, string key, DateTime expiration)
        {
            TrackNewEntry(cleaner, key, expiration, out var _);
        }
    }
}
