namespace Celestus.Storage.Cache.Test.Model
{
    public static class CacheHelper
    {
        public static CacheBase<string, string> GetOrCreateShared(Type cacheType, string key, bool persistenceEnabled = false, string persistenceStorageLocation = "")
        {
            if (typeof(ThreadCache) == cacheType)
            {
                return ThreadCache.Factory.GetOrCreateShared(key, persistenceEnabled, persistenceStorageLocation);
            }
            else
            {
                return Cache.Factory.GetOrCreateShared(key, persistenceEnabled, persistenceStorageLocation);
            }
        }

        public static CacheBase<string, string>? TryCreateFromFile(Type cacheType, Uri path)
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

        public static CacheBase<string, string> Create(Type cacheType, string key, bool persistenceEnabled = false, string persistenceStorageLocation = "")
        {
            return (CacheBase<string, string>)Activator.CreateInstance(cacheType, [key, persistenceEnabled, persistenceStorageLocation])!;
        }
    }
}