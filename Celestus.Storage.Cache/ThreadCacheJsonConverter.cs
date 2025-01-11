using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class ThreadCacheJsonConverter : JsonConverter<ThreadCache>
    {
        public override ThreadCache? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid JSON for {nameof(ThreadCache)}.");
            }

            string? key = null;
            Cache? cache = null;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        break;

                    case JsonTokenType.PropertyName:
                        if (reader.GetString() is not string propertyName)
                        {
                            continue;
                        }

                        _ = reader.Read();

                        switch (propertyName)
                        {
                            case nameof(ThreadCache.Key):
                                key = reader.GetString();
                                break;

                            case nameof(ThreadCache._cache):
                                cache = JsonSerializer.Deserialize<Cache>(ref reader, options);
                                break;
                        }

                        break;
                }
            }

            if (key == null || cache == null)
            {
                throw new JsonException($"Invalid JSON for {nameof(ThreadCache)}.");
            }

            return new ThreadCache(key, cache);
        }

        public override void Write(Utf8JsonWriter writer, ThreadCache value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(ThreadCache.Key), value.Key);
            writer.WritePropertyName(nameof(ThreadCache._cache));
            JsonSerializer.Serialize(writer, value._cache, options);

            writer.WriteEndObject();
        }
    }
}
