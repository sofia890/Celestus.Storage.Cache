using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public class StartTokenJsonException(JsonTokenType expected, JsonTokenType encountered) : Exception($"Expected '{expected}' encountered '{encountered}'.");

    public class PropertiesOutOfOrderJsonException(string first, string then) : Exception($"Expected '{first}' before '{then}'.");

    public class MissingValueJsonException(string message) : Exception(message);

    public class ObjectCorruptJsonException(string property) : Exception($"Could not deserialize value stored in property {property}.");

    public class ValueTypeJsonException(string property, JsonTokenType expected, JsonTokenType encountered) : Exception($"Expected property '{property}' to hold value of type '{expected}' but encountered '{encountered}'.");

    public class NotObjectTypeJsonException(string property, string value) : Exception($"Expected a known '{typeof(Type)}' encountered '{value}' in property '{property}'.");

    public class MissingInheritanceJsonException(string property, object? obj, Type expectedAncestor) : Exception($"Expected '{obj}' to inherit from '{expectedAncestor}' in property '{property}'.");
}
