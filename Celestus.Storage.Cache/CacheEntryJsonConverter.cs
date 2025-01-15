using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    internal class CacheEntryJsonConverter : JsonConverter<CacheEntry>
    {
        const string TYPE_PROPERTY_NAME = "Type";

        public override CacheEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Invalid JSON for {nameof(CacheEntry)}.");
            }

            Type? type = null;
            long expiration = 0;
            object? data = null;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        return new CacheEntry(expiration, data);

                    case JsonTokenType.PropertyName:
                        if (reader.GetString() is not string propertyName)
                        {
                            throw new JsonException($"Invalid JSON for {nameof(CacheEntry)}.");
                        }
                        else
                        {
                            _ = reader.Read();

                            switch (propertyName)
                            {
                                case TYPE_PROPERTY_NAME:
                                    if (JsonSerializer.Deserialize<string>(ref reader, options) is not string typeString)
                                    {
                                        throw new JsonException($"Invalid JSON for {nameof(CacheEntry)}.");
                                    }
                                    else if (Type.GetType(typeString) is not Type parsedType)
                                    {
                                        throw new JsonException($"Invalid JSON for {nameof(CacheEntry)}.");
                                    }
                                    else
                                    {
                                        type = parsedType;
                                    }
                                    break;

                                case nameof(CacheEntry.Expiration):
                                    expiration = reader.GetInt64();
                                    break;

                                case nameof(CacheEntry.Data):
                                    if (type == null)
                                    {
                                        throw new JsonException($"{TYPE_PROPERTY_NAME} has to be before {nameof(CacheEntry.Data)} " +
                                                                $"for {nameof(CacheEntry)}.");
                                    }

                                    data = JsonSerializer.Deserialize(ref reader, type, options);
                                    break;
                            }
                        }

                        break;
                }
            }

            throw new JsonException($"Invalid JSON for {nameof(CacheEntry)}.");
        }

        public override void Write(Utf8JsonWriter writer, CacheEntry value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(TYPE_PROPERTY_NAME);

            if (value.Data != null)
            {
                var type = value.Data.GetType();

                JsonSerializer.Serialize(writer, type.AssemblyQualifiedName, options);
            }
            else
            {
                JsonSerializer.Serialize(writer, string.Empty, options);
            }

            writer.WritePropertyName(nameof(CacheEntry.Expiration));
            writer.WriteNumberValue(value.Expiration);

            writer.WritePropertyName(nameof(CacheEntry.Data));
            JsonSerializer.Serialize(writer, value.Data, options);

            writer.WriteEndObject();
        }
    }
}
