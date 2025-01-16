using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class CacheJsonConverter : JsonConverter<Cache>
    {
        public override Cache Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
            }

            string? key = null;
            Dictionary<string, CacheEntry>? storage = null;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        break;

                    case JsonTokenType.PropertyName:
                        switch (reader.GetString())
                        {
                            case nameof(Cache.Key):
                                _ = reader.Read();

                                key = reader.GetString();
                                break;

                            case nameof(Cache._storage):
                                _ = reader.Read();

                                storage = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(ref reader, options);
                                break;

                            default:
                                throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                        }

                        break;
                }
            }

            if (key == null || storage == null)
            {
                throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
            }

            return new Cache(key, storage);
        }

        public override void Write(Utf8JsonWriter writer, Cache value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(Cache.Key), value.Key);

            writer.WritePropertyName(nameof(Cache._storage));
            JsonSerializer.Serialize(writer, value._storage, options);

            writer.WriteEndObject();
        }
    }
}
