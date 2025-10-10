using Celestus.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(CacheEntryJsonConverter))]
    public partial record CacheEntry(DateTime Expiration, object? Data)
    {
    }
}
