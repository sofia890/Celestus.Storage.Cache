using Celestus.Exceptions;
using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public class JsonException(string message) : Exception(message);

    public class WrongTokenJsonException(JsonTokenType expected, JsonTokenType encountered) : JsonException($"Expected '{expected}' encountered '{encountered}'.")
    {
        public static void Assert(JsonTokenType expected, JsonTokenType encountered)
        {
            if (expected != encountered)
            {
                throw new WrongTokenJsonException(expected, encountered);
            }
        }

        public static void ThrowIf(bool condition, string message)
        {
            Condition.ThrowIf<WrongTokenJsonException>(condition, message);
        }
    }

    public class PropertiesOutOfOrderJsonException(string first, string then) : JsonException($"Expected '{first}' before '{then}'.");

    public class MissingValueJsonException(string message) : JsonException(message);

    public class ObjectCorruptJsonException(string property) : JsonException($"Could not deserialize value stored in property {property}.");

    public class ValueTypeJsonException(string property, JsonTokenType expected, JsonTokenType encountered) : JsonException($"Expected property '{property}' to hold value of type '{expected}' but encountered '{encountered}'.");

    public class NotObjectTypeJsonException(string property, string value) : JsonException($"Expected a known '{typeof(Type)}' encountered '{value}' in property '{property}'.");

    public class MissingInheritanceJsonException(string property, object? obj, Type expectedAncestor) : JsonException($"Expected '{obj}' to inherit from '{expectedAncestor}' in property '{property}'.");

    public class JsonNullValueException(string message) : JsonException(message)
    {
        public static void ThrowIf(bool condition, string message)
        {
            Condition.ThrowIf<JsonNullValueException>(condition, message);
        }
    }
}
