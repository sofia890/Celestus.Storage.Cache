namespace Celestus.Storage.Cache.Test.Model
{
    public static class CacheHelper
    {
        public static CacheBase<string, string> GetOrCreateShared(Type cacheType, string id, bool persistenceEnabled = false, string persistenceStorageLocation = "")
        {
            if (typeof(ThreadSafeCache) == cacheType)
            {
                return ThreadSafeCache.Factory.GetOrCreateShared(id, persistenceEnabled, persistenceStorageLocation);
            }
            else
            {
                return Cache.Factory.GetOrCreateShared(id, persistenceEnabled, persistenceStorageLocation);
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

        public static CacheBase<string, string> Create(Type cacheType, string id, bool persistenceEnabled = false, FileInfo? persistenceStorageLocation = null)
        {
            return (CacheBase<string, string>)Activator.CreateInstance(cacheType, [id, persistenceEnabled, persistenceStorageLocation?.FullName ?? string.Empty])!;
        }
    }
}