
namespace Celestus.Storage.Cache.Test.Model
{
    internal class CacheCleanerContext
    {
        public Dictionary<string, CacheEntry> Storage { get; init; } = new();
    }
}
