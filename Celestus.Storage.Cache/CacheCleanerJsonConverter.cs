using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    internal class CacheCleanerJsonConverter : JsonConverter<CacheCleaner<string, string>>
    {
        private const string CLEANUP_INTERVAL_PROPERTY_NAME = "CleanupInterval";

        public override CacheCleaner<string, string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            CacheCleaner<string, string>? cacheCleaner = null;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        goto End;

                    case JsonTokenType.PropertyName:
                        var propertyName = reader.GetString();
                        _ = reader.Read();

                        switch (propertyName)
                        {
                            case CLEANUP_INTERVAL_PROPERTY_NAME:
                                cacheCleaner = new(JsonSerializer.Deserialize<TimeSpan>(ref reader, options));
                                break;

                            default:
                                reader.Skip();
                                break;
                        }
                        break;

                    default:
                        reader.Skip();
                        break;
                }
            }
        End:
            if (cacheCleaner == null)
            {
                throw new MissingValueJsonException($"Invalid JSON for {nameof(CacheCleaner<string, string>)}.");
            }
            else
            {
                return cacheCleaner;
            }
        }

        public override void Write(Utf8JsonWriter writer, CacheCleaner<string, string> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(CLEANUP_INTERVAL_PROPERTY_NAME);
            JsonSerializer.Serialize(writer, value.GetCleaningInterval(), options);
            writer.WriteEndObject();
        }
    }
}
