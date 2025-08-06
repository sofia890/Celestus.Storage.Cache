
using System.Text.Json;

namespace Celestus.Serialization
{
    public static class Serialize
    {
        public static void SaveToFile<DataType>(DataType data, Uri path)
        {
            var serializedData = JsonSerializer.Serialize(data);
            File.WriteAllText(path.AbsolutePath, serializedData);
        }

        public static DataType? TryCreateFromFile<DataType>(Uri path)
            where DataType : class
        {
            try
            {
                var serializedData = File.ReadAllText(path.AbsolutePath);
                return JsonSerializer.Deserialize<DataType>(serializedData);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}