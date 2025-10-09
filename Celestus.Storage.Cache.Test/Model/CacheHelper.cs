namespace Celestus.Storage.Cache.Test.Model
{
    public static class CacheHelper
    {
        public static CacheBase<string, string> GetOrCreateShared(Type cacheType, string key, bool persistenceEnabled = false, string persistenceStorageLocation = "")
        {
            if (typeof(ThreadSafeCache) == cacheType)
            {
                return ThreadSafeCache.Factory.GetOrCreateShared(key, persistenceEnabled, persistenceStorageLocation);
            }
            else
            {
                return Cache.Factory.GetOrCreateShared(key, persistenceEnabled, persistenceStorageLocation);
            }
        }

        public static CacheBase<string, string>? TryCreateFromFile(Type cacheType, FileInfo file)
        {
            if (typeof(ThreadSafeCache) == cacheType)
            {
                return ThreadSafeCache.TryCreateFromFile(file);
            }
            else
            {
                return Cache.TryCreateFromFile(file);
            }
        }

        public static CacheBase<string, string> Create(Type cacheType, string key, bool persistenceEnabled = false, string persistenceStorageLocation = "")
        {
            return (CacheBase<string, string>)Activator.CreateInstance(cacheType, [key, persistenceEnabled, persistenceStorageLocation])!;
        }
    }
}