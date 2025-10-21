using System.Text.Json;

namespace Celestus.Storage.Cache.Test.Model
{
    internal class CacheEntryHelper
    {
        public static JsonSerializerOptions CreateOptions(BlockedEntryBehavior behavior)
        {
            var options = new JsonSerializerOptions();
            options.SetBlockedEntryBehavior(behavior);
            options.Converters.Add(new CacheEntry.CacheEntryJsonConverter());

            return options;
        }

        public static CacheEntry Deserialize(string json, BlockedEntryBehavior behavior)
        {
            return JsonSerializer.Deserialize<CacheEntry>(json, CreateOptions(behavior))!;
        }

        public static string Serialize(object? data)
        {
            var expiration = DateTime.MaxValue;
            var entry = new CacheEntry(expiration, data);

            var options = new JsonSerializerOptions();
            options.Converters.Add(new CacheEntry.CacheEntryJsonConverter());

            return JsonSerializer.Serialize(entry, options);
        }
    }
}
