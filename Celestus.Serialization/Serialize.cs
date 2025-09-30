using System.Text.Json;

namespace Celestus.Serialization
{
    public static class Serialize
    {
        public static void SaveToFile<DataType>(DataType data, Uri path)
        {
            var filePath = path.AbsolutePath;
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var serializedData = JsonSerializer.Serialize(data);
            File.WriteAllText(filePath, serializedData);
        }

        public static DataType? TryCreateFromFile<DataType>(Uri path)
            where DataType : class
        {
            try
            {
                var serializedData = File.ReadAllText(path.AbsolutePath);
                return JsonSerializer.Deserialize<DataType>(serializedData);
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