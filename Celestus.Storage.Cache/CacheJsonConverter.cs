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

            string? key = null;
            Dictionary<string, CacheEntry>? storage = null;
            CacheCleanerBase<string>? cleaner = null;
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
                            case nameof(Cache.Key):
                                key = GetKey(ref reader);
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
            ValidateConfiguration(key, storage, cleaner, cleanerConfigured);

            return new Cache(key, storage, cleaner);
        }

        private static string? GetKey(ref Utf8JsonReader reader)
        {
            _ = reader.Read();
            return reader.GetString();
        }

        private static Dictionary<string, CacheEntry>? GetStorage(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            _ = reader.Read();
            return JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(ref reader, options);
        }

        private static (CacheCleanerBase<string>? cleaner, bool cleanerConfigured) GetCleaner(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            CacheCleanerBase<string>? cleaner = null;
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

        private static CacheCleanerBase<string>? CreateCleaner(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (JsonSerializer.Deserialize<string>(ref reader, options) is not string typeString)
            {
                throw new ValueTypeJsonException(TYPE_PROPERTY_NAME, JsonTokenType.String, reader.TokenType);
            }
            else if (Type.GetType(typeString) is not Type cleanerType)
            {
                throw new NotObjectTypeJsonException(TYPE_PROPERTY_NAME, typeString);
            }
            else if (Activator.CreateInstance(cleanerType) is not CacheCleanerBase<string> createdCleaner)
            {
                throw new MissingInheritanceJsonException(TYPE_PROPERTY_NAME, cleanerType, typeof(CacheCleanerBase<string>));
            }
            else
            {
                return createdCleaner;
            }
        }

        private static void ValidateConfiguration(
            [NotNull] string? key,
            [NotNull] Dictionary<string, CacheEntry>? storage,
            [NotNull] CacheCleanerBase<string>? cleaner,
            bool cleanerConfigured)
        {
            Condition.ThrowIf<MissingValueJsonException>(key == null, nameof(Cache.Key));
            Condition.ThrowIf<MissingValueJsonException>(storage == null, nameof(Cache.Storage));
            Condition.ThrowIf<MissingValueJsonException>(cleaner == null, nameof(Cache.Cleaner));
            Condition.ThrowIf<MissingValueJsonException>(!cleanerConfigured, CONTENT_PROPERTY_NAME);
        }

        public override void Write(Utf8JsonWriter writer, Cache value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(nameof(Cache.Key), value.Key);

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
