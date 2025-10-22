namespace Celestus.Storage.Cache.Test.Model
{
    internal static class CleanerHelper
    {
        public static void AddEntryToCache(string key, TimeSpan duration, MockCache cache, out CacheEntry entry)
        {
            lock (cache)
            {
                cache.Set<object?>(key, null, duration);

                entry = cache.GetEntry(key);
            }
        }

        public static void AddEntryToCache(string key, TimeSpan duration, MockCache cache)
        {
            AddEntryToCache(key, duration, cache, out var _);
        }
    }
}
