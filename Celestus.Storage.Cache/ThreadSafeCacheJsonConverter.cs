using Celestus.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class ThreadSafeCacheJsonConverter : JsonConverter<ThreadSafeCache>
    {
        const int DEFAULT_LOCK_TIMEOUT = 10000;

        public override ThreadSafeCache? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Condition.ThrowIf<StartTokenJsonException>(
                reader.TokenType != JsonTokenType.StartObject,
                parameters: [reader.TokenType, JsonTokenType.StartObject]);

            string? id = null;
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
                            case nameof(ThreadSafeCache.Id):
                                _ = reader.Read();

                                id = reader.GetString();
                                break;

                            case nameof(ThreadSafeCache.Cache):
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
            if (id == null || cache == null)
            {
                throw new MissingValueJsonException($"Invalid JSON for {nameof(ThreadSafeCache)}.");
            }
            else
            {
                return new ThreadSafeCache(id, cache);
            }
        }

        public override void Write(Utf8JsonWriter writer, ThreadSafeCache value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(ThreadSafeCache.Id), value.Id);

            writer.WriteBoolean(nameof(ThreadSafeCache.PersistenceEnabled), value.PersistenceEnabled);

            if (value.PersistenceStorageFile != null)
            {
                writer.WriteString(nameof(ThreadSafeCache.PersistenceStorageFile),
                                   value.PersistenceStorageFile.FullName);
            }

            writer.WritePropertyName(nameof(ThreadSafeCache.Cache));
            JsonSerializer.Serialize(writer, value.Cache, options);

            writer.WriteEndObject();
        }
    }
}
