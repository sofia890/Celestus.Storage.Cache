using System.Text;
using System.Text.Json;

namespace Celestus.Storage.Cache.Test.Model
{
    public static class CleaningHelper
    {
        public static void ReadSettings<CleanerType>(string json)
            where CleanerType : CacheCleanerBase<string>, new()
        {
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));

            CleanerType cleaner = new();
            cleaner.ReadSettings(ref reader, new());
        }

        public static void ReadSettings(CacheCleanerBase<string> cleaner, string json)
        {
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));

            cleaner.ReadSettings(ref reader, new());
        }
    }
}
