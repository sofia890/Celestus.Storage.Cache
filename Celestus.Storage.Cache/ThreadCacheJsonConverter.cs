using Celestus.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class ThreadCacheJsonConverter : JsonConverter<ThreadCache>
    {
        const int DEFAULT_LOCK_TIMEOUT = 10000;

        public override ThreadCache? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Condition.ThrowIf<StartTokenJsonException>(
                reader.TokenType != JsonTokenType.StartObject,
                parameters: [reader.TokenType, JsonTokenType.StartObject]);

            string? key = null;
            Cache? cache = null;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    default:
                    case JsonTokenType.EndObject:
                        goto End;

                    case JsonTokenType.PropertyName:
                        switch (reader.GetString())
                        {
                            case nameof(ThreadCache.Key):
                                _ = reader.Read();

                                key = reader.GetString();
                                break;

                            case nameof(ThreadCache.Cache):
                                _ = reader.Read();

                                cache = JsonSerializer.Deserialize<Cache>(ref reader, options);
                                break;

                            default:
                                reader.Skip();
                                break;
                        }

                        break;
                }
            }

        End:
            if (key == null || cache == null)
            {
                throw new MissingValueJsonException($"Invalid JSON for {nameof(ThreadCache)}.");
            }

            return new ThreadCache(key, cache);
        }

        public override void Write(Utf8JsonWriter writer, ThreadCache value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(ThreadCache.Key), value.Key);
            writer.WritePropertyName(nameof(ThreadCache.Cache));

            JsonSerializer.Serialize(writer, value.Cache, options);

            writer.WriteEndObject();
        }
    }
}
