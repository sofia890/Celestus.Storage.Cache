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

            ICacheBase<string, string>? cache = null;

            var blockedBehavior = options.GetBlockedEntryBehavior();
            var register = options.GetCacheTypeRegister();

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
                                cache = JsonConverterHelper.DeserializeTypedObject<ICacheBase<string, string>>(ref reader, options);
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
                if (cache is Cache concreteCache)
                {
                    concreteCache.BlockedEntryBehavior = blockedBehavior;

                    if (blockedBehavior == BlockedEntryBehavior.Throw)
                    {
                        foreach (var e in concreteCache.Storage)
                        {
                            var dataType = e.Value.Data?.GetType();
                            if (dataType != null && !register.IsAllowed(dataType))
                            {
                                throw new BlockedCacheTypeException(dataType, "thread-safe cache deserialization");
                            }
                        }
                    }
                }

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
