using System.Diagnostics.CodeAnalysis;

namespace Celestus.Storage.Cache
{
    public interface ICacheManager<CacheKeyType, CacheType>
    {
        public bool TryLoad(CacheKeyType key, [NotNullWhen(true)] out CacheType? cache);

        public CacheType GetOrCreateShared(CacheKeyType key, bool persistent = false, string persistentStorageLocation = "");

        public CacheType? UpdateOrLoadSharedFromFile(Uri path, TimeSpan? timeout = null);

        public void SetCleanupInterval(TimeSpan interval);

        public void Remove(CacheKeyType key);
    }
}
