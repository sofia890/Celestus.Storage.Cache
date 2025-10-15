using Celestus.Exceptions;
using System.Text.Json;

namespace Celestus.Storage.Cache
{
    internal class JsonConverterHelper
    {
        const string TYPE_PROPERTY_NAME = "Type";
        const string CONTENT_PROPERTY_NAME = "Content";

        public static ObjectType? DeserializeTypedObject<ObjectType>(ref Utf8JsonReader reader, JsonSerializerOptions options)
            where ObjectType : class
        {
            Condition.ThrowIf<WrongTokenJsonException>(
                reader.TokenType != JsonTokenType.StartObject,
                parameters: [JsonTokenType.StartObject, reader.TokenType]);

            Type? objectType = null;
            ObjectType? objectInstance = null;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    default:
                        break;

                    case JsonTokenType.EndObject:
                        goto CleanerDone;

                    case JsonTokenType.StartObject:
                        break;

                    case JsonTokenType.PropertyName:
                        switch (reader.GetString())
                        {
                            case TYPE_PROPERTY_NAME:
                                _ = reader.Read();

                                if (JsonSerializer.Deserialize<string>(ref reader, options) is not string typeString)
                                {
                                    throw new ValueTypeJsonException(TYPE_PROPERTY_NAME, JsonTokenType.String, reader.TokenType);
                                }
                                else if (Type.GetType(typeString) is not Type parsedObjectType)
                                {
                                    throw new NotObjectTypeJsonException(TYPE_PROPERTY_NAME, typeString);
                                }
                                else
                                {
                                    objectType = parsedObjectType;
                                }
                                break;

                            case CONTENT_PROPERTY_NAME:
                                _ = reader.Read();

                                if (objectType == null)
                                {
                                    throw new PropertiesOutOfOrderJsonException(TYPE_PROPERTY_NAME, CONTENT_PROPERTY_NAME);
                                }
                                else
                                {
                                    var deserializedObject = JsonSerializer.Deserialize(ref reader, objectType, options);

                                    if (deserializedObject is not ObjectType typedObject)
                                    {
                                        throw new MissingInheritanceJsonException(CONTENT_PROPERTY_NAME, deserializedObject, typeof(ObjectType));
                                    }
                                    else
                                    {
                                        objectInstance = typedObject;
                                    }
                                }

                                break;

                            default:
                                _ = reader.Read();
                                break;
                        }
                        break;
                }
            }

        CleanerDone:
            return objectInstance;
        }

        public static void SerializeTypedObject(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            JsonNullValueException.ThrowIf(value == null, "Could not determine type while serializing.");

            var type = value!.GetType();

            writer.WritePropertyName(TYPE_PROPERTY_NAME);
            writer.WriteStringValue(type?.AssemblyQualifiedName);

            writer.WritePropertyName(CONTENT_PROPERTY_NAME);
            JsonSerializer.Serialize(writer, value, type!, options);

            writer.WriteEndObject();
        }
    }
}
