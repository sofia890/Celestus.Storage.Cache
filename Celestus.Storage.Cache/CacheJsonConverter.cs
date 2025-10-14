using Celestus.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class CacheJsonConverter : JsonConverter<Cache>
    {
        public override Cache Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Condition.ThrowIf<WrongTokenJsonException>(
                reader.TokenType != JsonTokenType.StartObject,
                parameters: [reader.TokenType, JsonTokenType.StartObject]);

            string? id = null;
            bool persistenceEnabled = false;
            string? persistenceStorageLocation = null;
            Dictionary<string, CacheEntry>? storage = null;
            CacheCleanerBase<string, string>? cleaner = null;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    default:
                    case JsonTokenType.EndObject:
                        goto End;

                    case JsonTokenType.PropertyName:
                        var name = reader.GetString();
                        _ = reader.Read();

                        switch (name)
                        {
                            case nameof(Cache.Id):
                                id = reader.GetString();
                                break;

                            case nameof(ThreadSafeCache.PersistenceEnabled):
                                persistenceEnabled = reader.GetBoolean();
                                break;

                            case nameof(ThreadSafeCache.PersistenceStorageFile):
                                persistenceStorageLocation = reader.GetString();
                                break;

                            case nameof(Storage):
                                storage = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(ref reader, options);
                                break;

                            case nameof(Cache.Cleaner):
                                cleaner = JsonConverterHelper.DeserializeTypedObject<CacheCleanerBase<string, string>>(ref reader, options);
                                break;

                            default:
                                reader.Skip();
                                break;
                        }
                        break;
                }
            }

        End:
            ValidateConfiguration(id, storage, cleaner);

            return new Cache(id,
                             storage,
                             cleaner,
                             persistenceEnabled: persistenceEnabled,
                             persistenceStorageLocation: persistenceStorageLocation ?? "",
                             persistenceLoadWhenCreated: false);
        }

        private static void ValidateConfiguration(
            [NotNull] string? id,
            [NotNull] Dictionary<string, CacheEntry>? storage,
            [NotNull] CacheCleanerBase<string, string>? cleaner)
        {
            Condition.ThrowIf<MissingValueJsonException>(id == null, nameof(Cache.Id));
            Condition.ThrowIf<MissingValueJsonException>(storage == null, nameof(Cache.Storage));
            Condition.ThrowIf<MissingValueJsonException>(cleaner == null, nameof(Cache.Cleaner));
        }

        public override void Write(Utf8JsonWriter writer, Cache value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(Cache.Id), value.Id);

            writer.WriteBoolean(nameof(ThreadSafeCache.PersistenceEnabled), value.PersistenceEnabled);

            if (value.PersistenceStorageFile != null)
            {
                writer.WriteString(nameof(ThreadSafeCache.PersistenceStorageFile),
                                   value.PersistenceStorageFile.FullName);
            }

            writer.WritePropertyName(nameof(Cache.Storage));
            JsonSerializer.Serialize(writer, value.Storage, options);

            writer.WritePropertyName(nameof(Cache.Cleaner));
            JsonConverterHelper.SerializeTypedObject(writer, value.Cleaner, options);

            writer.WriteEndObject();
        }
    }
}
