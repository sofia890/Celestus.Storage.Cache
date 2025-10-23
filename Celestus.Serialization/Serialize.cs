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

        public static bool TrySaveToFile<DataType>(DataType data, FileInfo file, JsonSerializerOptions? options = null)
        {
            return TrySaveToFile(data, file, out _, options);
        }

        public static bool TrySaveToFile<DataType>(DataType data, FileInfo file, out Exception? error, JsonSerializerOptions? options = null)
        {
            try
            {
                SaveToFile(data, file, options);

                error = null;

                return true;
            }
            catch (Exception exception)
            {
                error = exception;

                return false;
            }
        }

        public static DataType? TryCreateFromFile<DataType>(FileInfo file, JsonSerializerOptions? options = null)
            where DataType : class
        {
            return TryCreateFromFile<DataType>(file, out _, options);
        }

        public static DataType? TryCreateFromFile<DataType>(FileInfo file, out Exception? error, JsonSerializerOptions? options = null)
            where DataType : class
        {
            try
            {
                error = null;

                var serializedData = File.ReadAllText(file.FullName);

                return JsonSerializer.Deserialize<DataType>(serializedData, options ?? new());
            }
            catch (Exception ex)
            {
                error = ex;

                return null;
            }
        }
    }
}