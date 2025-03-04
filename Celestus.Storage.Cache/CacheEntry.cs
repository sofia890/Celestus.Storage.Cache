using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(CacheEntryJsonConverter))]
    public record CacheEntry(long Expiration, object? Data)
    {
        public class CacheEntryJsonConverter : JsonConverter<CacheEntry>
        {
            const string TYPE_PROPERTY_NAME = "Type";

            public override CacheEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException($"Invalid JSON for {nameof(CacheEntry)}.");
                }

                Type? type = null;
                long? expiration = null;
                bool dataSet = false;
                object? data = null;

                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        default:
                        case JsonTokenType.EndObject:
                            goto End;

                        case JsonTokenType.PropertyName:
                            var propertyName = reader.GetString();

                            _ = reader.Read();

                            switch (propertyName)
                            {
                                case TYPE_PROPERTY_NAME:
                                    if (JsonSerializer.Deserialize<string>(ref reader, options) is not string typeString)
                                    {
                                        throw new ValueTypeJsonException(TYPE_PROPERTY_NAME, JsonTokenType.String, reader.TokenType);
                                    }
                                    else if (Type.GetType(typeString) is not Type parsedType)
                                    {
                                        throw new NotObjectTypeJsonException(TYPE_PROPERTY_NAME, typeString);
                                    }
                                    else
                                    {
                                        type = parsedType;
                                    }
                                    break;

                                case nameof(Expiration):
                                    expiration = reader.GetInt64();
                                    break;

                                case nameof(Data):
                                    if (type == null)
                                    {
                                        throw new PropertiesOutOfOrderJsonException(nameof(TYPE_PROPERTY_NAME), nameof(Data));
                                    }
                                    else if (reader.TokenType == JsonTokenType.Null)
                                    {
                                        dataSet = true;
                                    }
                                    else if (JsonSerializer.Deserialize(ref reader, type, options) is not object newData)
                                    {
                                        throw new ObjectCorruptJsonException(nameof(Data));
                                    }
                                    else
                                    {
                                        dataSet = true;
                                        data = newData;
                                    }
                                    break;

                                default:
                                    reader.Skip();
                                    break;
                            }

                            break;
                    }
                }

            End:
                if (expiration == null)
                {
                    throw new MissingValueJsonException(nameof(expiration));
                }
                else if (type == null)
                {
                    throw new MissingValueJsonException(TYPE_PROPERTY_NAME);
                }
                else if (data == null && !dataSet)
                {
                    throw new MissingValueJsonException(nameof(data));
                }
                else
                {
                    return new CacheEntry((long)expiration, data);
                }
            }

            public override void Write(Utf8JsonWriter writer, CacheEntry value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WritePropertyName(TYPE_PROPERTY_NAME);

                if (value.Data != null)
                {
                    var type = value.Data.GetType();

                    writer.WriteStringValue(type.AssemblyQualifiedName);
                }
                else
                {
                    writer.WriteNullValue();
                }

                writer.WritePropertyName(nameof(Expiration));
                writer.WriteNumberValue(value.Expiration);

                writer.WritePropertyName(nameof(Data));
                JsonSerializer.Serialize(writer, value.Data, options);

                writer.WriteEndObject();
            }
        }
    }
}
