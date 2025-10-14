using Celestus.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class ThreadSafeCacheJsonConverter : JsonConverter<ThreadSafeCache>
    {
        public override ThreadSafeCache? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Condition.ThrowIf<WrongTokenJsonException>(
                reader.TokenType != JsonTokenType.StartObject,
                parameters: [reader.TokenType, JsonTokenType.StartObject]);

            CacheBase<string, string>? cache = null;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    default:
                    case JsonTokenType.EndObject:
                        goto End;

                    case JsonTokenType.StartObject:
                        break;

                    case JsonTokenType.PropertyName:
                        switch (reader.GetString())
                        {
                            case nameof(ThreadSafeCache.Cache):
                                _ = reader.Read();
                                cache = JsonConverterHelper.DeserializeTypedObject<CacheBase<string, string>>(ref reader, options);
                                break;

                            default:
                                reader.Skip();
                                break;
                        }

                        break;
                }
            }

        End:
            if (cache == null)
            {
                throw new MissingValueJsonException($"Invalid JSON for {nameof(ThreadSafeCache)}. Missing {nameof(ThreadSafeCache.Cache)}.");
            }
            else
            {
                return new ThreadSafeCache(cache);
            }
        }

        public override void Write(Utf8JsonWriter writer, ThreadSafeCache value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteBoolean(nameof(ThreadSafeCache.PersistenceEnabled), value.PersistenceEnabled);

            if (value.PersistenceStorageFile != null)
            {
                writer.WriteString(nameof(ThreadSafeCache.PersistenceStorageFile),
                                   value.PersistenceStorageFile.FullName);
            }

            writer.WritePropertyName(nameof(ThreadSafeCache.Cache));
            JsonConverterHelper.SerializeTypedObject(writer, value.Cache, options);

            writer.WriteEndObject();
        }
    }
}
