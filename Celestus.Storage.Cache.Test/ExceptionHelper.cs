using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache.Test
{
    internal class ExceptionHelper
    {
        public static void Deserialize<ConverterType, DataType>(string json)
            where ConverterType : JsonConverter<DataType>, new()
        {
            JsonSerializerOptions _options = new()
            {
                Converters = { new ConverterType() }
            };

            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
            var converter = new ConverterType();

            reader.Read();

            converter.Read(ref reader, typeof(DataType), _options);
        }
    }
}
