using Celestus.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class CacheJsonConverter : JsonConverter<Cache>
    {
        const string TYPE_PROPERTY_NAME = "Type";
        const string CONTENT_PROPERTY_NAME = "Content";

        public override Cache Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Condition.ThrowIf<StartTokenJsonException>(
                reader.TokenType != JsonTokenType.StartObject,
                parameters: [reader.TokenType, JsonTokenType.StartObject]);

            string? id = null;
            bool persistenceEnabled = false;
            string? persistenceStorageLocation = null;
            Dictionary<string, CacheEntry>? storage = null;
            CacheCleanerBase<string, string>? cleaner = null;
            bool cleanerConfigured = false;

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
                            case nameof(Cache.Id):
                                _ = reader.Read();

                                id = reader.GetString();
                                break;

                            case nameof(ThreadCache.PersistenceEnabled):
                                _ = reader.Read();

                                persistenceEnabled = reader.GetBoolean();
                                break;

                            case nameof(ThreadCache.PersistenceStoragePath):
                                _ = reader.Read();

                                persistenceStorageLocation = reader.GetString();
                                break;

                            case nameof(Storage):
                                storage = GetStorage(ref reader, options);
                                break;

                            case nameof(Cache.Cleaner):
                                (cleaner, cleanerConfigured) = GetCleaner(ref reader, options);
                                break;

                            default:
                                reader.Skip();
                                break;
                        }
                        break;
                }
            }

        End:
            ValidateConfiguration(id, storage, cleaner, cleanerConfigured);

            return new Cache(id,
                             storage,
                             cleaner,
                             persistenceEnabled: persistenceEnabled,
                             persistenceStorageLocation: persistenceStorageLocation ?? "",
                             persistenceLoadWhenCreated: false);
        }

        private static Dictionary<string, CacheEntry>? GetStorage(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            _ = reader.Read();
            return JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(ref reader, options);
        }

        private static (CacheCleanerBase<string, string>? cleaner, bool cleanerConfigured) GetCleaner(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            CacheCleanerBase<string, string>? cleaner = null;
            bool cleanerConfigured = false;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    default:
                    case JsonTokenType.EndObject:
                        goto CleanerDone;

                    case JsonTokenType.StartObject:
                        break;

                    case JsonTokenType.PropertyName:
                        switch (reader.GetString())
                        {
                            case TYPE_PROPERTY_NAME:
                                cleaner = CreateCleaner(ref reader, options);
                                break;

                            case CONTENT_PROPERTY_NAME:
                                if (cleaner == null)
                                {
                                    throw new PropertiesOutOfOrderJsonException(TYPE_PROPERTY_NAME, CONTENT_PROPERTY_NAME);
                                }
                                else
                                {
                                    cleaner.ReadSettings(ref reader, options);
                                    cleanerConfigured = true;
                                }
                                break;

                            default:
                                reader.Skip();
                                break;
                        }
                        break;
                }
            }

        CleanerDone:
            return (cleaner, cleanerConfigured);
        }

        private static CacheCleanerBase<string, string>? CreateCleaner(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (JsonSerializer.Deserialize<string>(ref reader, options) is not string typeString)
            {
                throw new ValueTypeJsonException(TYPE_PROPERTY_NAME, JsonTokenType.String, reader.TokenType);
            }
            else if (Type.GetType(typeString) is not Type cleanerType)
            {
                throw new NotObjectTypeJsonException(TYPE_PROPERTY_NAME, typeString);
            }
            else if (Activator.CreateInstance(cleanerType) is not CacheCleanerBase<string, string> createdCleaner)
            {
                throw new MissingInheritanceJsonException(TYPE_PROPERTY_NAME, cleanerType, typeof(CacheCleanerBase<string, string>));
            }
            else
            {
                return createdCleaner;
            }
        }

        private static void ValidateConfiguration(
            [NotNull] string? id,
            [NotNull] Dictionary<string, CacheEntry>? storage,
            [NotNull] CacheCleanerBase<string, string>? cleaner,
            bool cleanerConfigured)
        {
            Condition.ThrowIf<MissingValueJsonException>(id == null, nameof(Cache.Id));
            Condition.ThrowIf<MissingValueJsonException>(storage == null, nameof(Cache.Storage));
            Condition.ThrowIf<MissingValueJsonException>(cleaner == null, nameof(Cache.Cleaner));
            Condition.ThrowIf<MissingValueJsonException>(!cleanerConfigured, CONTENT_PROPERTY_NAME);
        }

        public override void Write(Utf8JsonWriter writer, Cache value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(Cache.Id), value.Id);

            writer.WriteBoolean(nameof(ThreadCache.PersistenceEnabled), value.PersistenceEnabled);

            if (value.PersistenceStoragePath != null)
            {
                writer.WriteString(nameof(ThreadCache.PersistenceStoragePath),
                                   value.PersistenceStoragePath.OriginalString);
            }

            writer.WritePropertyName(nameof(Storage));
            JsonSerializer.Serialize(writer, value.Storage, options);

            writer.WritePropertyName(nameof(Cache.Cleaner));
            writer.WriteStartObject();

            var type = value.Cleaner.GetType();
            writer.WritePropertyName(TYPE_PROPERTY_NAME);
            writer.WriteStringValue(type.AssemblyQualifiedName);

            writer.WritePropertyName(CONTENT_PROPERTY_NAME);
            value.Cleaner.WriteSettings(writer, options);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
    }
}
