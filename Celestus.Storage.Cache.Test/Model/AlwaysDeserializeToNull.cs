using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache.Test.Model
{
    [JsonConverter(typeof(AlwaysDeserializeToNull))]
    internal class AlwaysDeserializeToNull : JsonConverter<AlwaysDeserializeToNull>
    {
        public override AlwaysDeserializeToNull? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return null;
        }

        public override void Write(Utf8JsonWriter writer, AlwaysDeserializeToNull value, JsonSerializerOptions options)
        {
        }
    }
}
