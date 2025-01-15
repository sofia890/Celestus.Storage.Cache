using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class CacheJsonConverter : JsonConverter<Cache>
    {
        public override Cache Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var storage = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(ref reader, options);

            if (storage == null)
            {
                throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
            }
            else
            {
                return new Cache(storage);
            }
        }

        public override void Write(Utf8JsonWriter writer, Cache value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value._storage, options);
        }
    }
}
