using System.Text.Json;

namespace Celestus.Serialization
{
    public static class Serialize
    {
        public static void SaveToFile<DataType>(DataType data, FileInfo file, JsonSerializerOptions? options = null)
        {
            var filePath = file.FullName;
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var serializedData = JsonSerializer.Serialize(data, options ?? new());
            File.WriteAllText(filePath, serializedData);
        }

        public static DataType? TryCreateFromFile<DataType>(FileInfo file, JsonSerializerOptions? options = null)
            where DataType : class
        {
            try
            {
                var serializedData = File.ReadAllText(file.FullName);
                return JsonSerializer.Deserialize<DataType>(serializedData, options ?? new());
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}