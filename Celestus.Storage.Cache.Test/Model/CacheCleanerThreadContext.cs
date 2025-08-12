
namespace Celestus.Storage.Cache.Test.Model
{
    internal class CacheCleanerThreadContext
    {
        public Dictionary<string, CacheEntry> Storage { get; init; } = new();
    }
}
