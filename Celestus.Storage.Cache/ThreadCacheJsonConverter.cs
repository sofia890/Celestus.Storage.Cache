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
                            case nameof(ThreadCache.Id):
                                _ = reader.Read();

                                id = reader.GetString();
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
            if (id == null || cache == null)
            {
                throw new MissingValueJsonException($"Invalid JSON for {nameof(ThreadCache)}.");
            }
            else
            {
                return new ThreadCache(id, cache);
            }
        }

        public override void Write(Utf8JsonWriter writer, ThreadCache value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(ThreadCache.Id), value.Id);

            writer.WriteBoolean(nameof(ThreadCache.PersistenceEnabled), value.PersistenceEnabled);

            if (value.PersistenceStorageFile != null)
            {
                writer.WriteString(nameof(ThreadCache.PersistenceStorageFile),
                                   value.PersistenceStorageFile.FullName);
            }

            writer.WritePropertyName(nameof(ThreadCache.Cache));
            JsonSerializer.Serialize(writer, value.Cache, options);

            writer.WriteEndObject();
        }
    }
}
