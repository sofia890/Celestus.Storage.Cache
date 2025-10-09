using System.Diagnostics.CodeAnalysis;

namespace Celestus.Storage.Cache
{
    public interface ICacheManager<CacheKeyType, CacheType>
    {
        public bool TryLoad(CacheKeyType key, [NotNullWhen(true)] out CacheType? cache);

        public CacheType GetOrCreateShared(CacheKeyType key, bool persistenceEnabled = false, string persistenceStorageLocation = "", TimeSpan? timeout = null);

        public CacheType? UpdateOrLoadSharedFromFile(FileInfo file, TimeSpan? timeout = null);

        public TimeSpan GetCleanupInterval();

        public void SetCleanupInterval(TimeSpan interval);

        public void Remove(CacheKeyType key);
    }
}
