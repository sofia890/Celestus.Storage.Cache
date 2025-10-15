using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(CacheEntryJsonConverter))]
    public partial record CacheEntry(DateTime Expiration, object? Data)
    {
    }
}
