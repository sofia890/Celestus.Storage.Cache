using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace Celestus.Storage.Cache.Test.Model
{
    public static class CleaningHelper
    {
        public static void Deserialize<CleanerType>(string json)
            where CleanerType : CacheCleanerBase<string, string>
        {
            JsonSerializer.Deserialize<CleanerType>(json);
        }

        public static bool Deserialize(string json, Type type)
        {
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));

            if (JsonSerializer.Deserialize(ref reader, type) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
