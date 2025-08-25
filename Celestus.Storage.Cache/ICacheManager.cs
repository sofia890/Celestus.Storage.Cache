using System.Diagnostics.CodeAnalysis;

namespace Celestus.Storage.Cache
{
    public interface ICacheManager<CacheType>
    {
        public bool TryLoad(string key, [NotNullWhen(true)] out CacheType? cache);

        public CacheType GetOrCreateShared(string key = "");

        public CacheType? UpdateOrLoadSharedFromFile(Uri path, TimeSpan? timeout = null);

        public void SetCleanupInterval(TimeSpan interval);

        public void Remove(string key);
    }
}
