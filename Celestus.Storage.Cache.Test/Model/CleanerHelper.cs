namespace Celestus.Storage.Cache.Test.Model
{
    internal static class CleanerHelper
    {
        public static void AddEntryToCache(string key, DateTime expiration, MockCache cache, out CacheEntry entry)
        {
            entry = new(expiration, null);

            lock (cache)
            {
                cache.Storage.Add(key, new(expiration, new()));
            }
        }

        public static void AddEntryToCache(string key, DateTime expiration, MockCache cache)
        {
            AddEntryToCache(key, expiration, cache, out var _);
        }
    }
}
