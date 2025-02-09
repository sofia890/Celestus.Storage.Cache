﻿using System.Text.Json;
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

                                case nameof(Expiration):
                                    expiration = reader.GetInt64();
                                    break;

                                case nameof(Data):
                                    if (type == null)
                                    {
                                        throw new JsonException($"{TYPE_PROPERTY_NAME} has to be before {nameof(Data)} " +
                                                                $"for {nameof(CacheEntry)}.");
                                    }

                                    data = JsonSerializer.Deserialize(ref reader, type, options);
                                    break;

                                default:
                                    throw new JsonException($"Invalid JSON for {nameof(CacheEntry)}.");
                            }

                            break;
                    }
                }

            End:
                if (expiration == null || data == null)
                {
                    throw new JsonException($"Invalid JSON for {nameof(CacheEntry)}.");
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
