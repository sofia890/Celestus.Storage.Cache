namespace Celestus.Storage.Cache.Test.Model
{
    public static class CacheHelper
    {
        public static CacheBase<string> GetOrCreateShared(Type cacheType, string key, bool persistent = false, string persistentStorageLocation = "")
        {
            if (typeof(ThreadCache) == cacheType)
            {
                return ThreadCache.Factory.GetOrCreateShared(key, persistent, persistentStorageLocation);
            }
            else
            {
                return Cache.Factory.GetOrCreateShared(key, persistent, persistentStorageLocation);
            }
        }

        public static CacheBase<string>? TryCreateFromFile(Type cacheType, Uri path)
        {
            if (typeof(ThreadCache) == cacheType)
            {
                return ThreadCache.TryCreateFromFile(path);
            }
            else
            {
                return Cache.TryCreateFromFile(path);
            }
        }

        public static CacheBase<string> Create(Type cacheType, string key, bool persistent = false, string persistentStorageLocation = "")
        {
            return (CacheBase<string>)Activator.CreateInstance(cacheType, [key, persistent, persistentStorageLocation])!;
        }
    }
}