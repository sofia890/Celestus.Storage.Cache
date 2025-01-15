using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(CacheEntryJsonConverter))]
    public record CacheEntry(long Expiration, object? Data);
}
