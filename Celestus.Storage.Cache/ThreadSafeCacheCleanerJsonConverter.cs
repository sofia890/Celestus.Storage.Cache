using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    internal class ThreadSafeCacheCleanerJsonConverter : JsonConverter<ThreadSafeCacheCleaner<string, string>>
    {
        const string INTERVAL_PROPERTY_NAME = "CleanupInterval";

        public override ThreadSafeCacheCleaner<string, string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ThreadSafeCacheCleaner<string, string> ? cacheCleaner = null;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    default:
                        break;

                    case JsonTokenType.EndObject:
                        goto End;

                    case JsonTokenType.StartObject:
                        break;

                    case JsonTokenType.PropertyName:
                        switch (reader.GetString())
                        {
                            case INTERVAL_PROPERTY_NAME:
                                _ = reader.Read();

                                cacheCleaner = new(JsonSerializer.Deserialize<TimeSpan>(ref reader, options));
                                break;

                            default:
                                break;

                        }
                        break;
                }
            }

        End:
            if (cacheCleaner == null)
            {
                throw new MissingValueJsonException($"Invalid JSON for {nameof(ThreadSafeCacheCleaner<string, string>)}.");
            }
            else
            {
                return cacheCleaner;
            }
        }

        public override void Write(Utf8JsonWriter writer, ThreadSafeCacheCleaner<string, string> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(INTERVAL_PROPERTY_NAME);
            JsonSerializer.Serialize(writer, value.GetCleaningInterval(), options);
            writer.WriteEndObject();
        }
    }
}
