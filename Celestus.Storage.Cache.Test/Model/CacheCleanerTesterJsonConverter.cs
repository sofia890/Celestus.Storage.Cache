using Celestus.Storage.Cache.Test.Model;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    internal class CacheCleanerTesterJsonConverter : JsonConverter<CacheCleanerTester>
    {
        public override CacheCleanerTester? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            WrongTokenJsonException.Assert(JsonTokenType.StartObject, reader.TokenType);
            WrongTokenJsonException.ThrowIf(!reader.Read(), "Unexpected end of JSON while reading CacheCleanerTester.");
            WrongTokenJsonException.Assert(JsonTokenType.EndObject, reader.TokenType);

            return new CacheCleanerTester
            {
                SettingsReadCorrectly = true
            };
        }

        public override void Write(Utf8JsonWriter writer, CacheCleanerTester value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            value.SettingsWritten = true;
            writer.WriteEndObject();
        }
    }
}
