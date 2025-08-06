using System.Text.Json;

namespace Celestus.Storage.Cache.Test.Model
{
    internal class CacheCleanerTester : CacheCleanerBase<string>
    {
        const string TEST_PROPERTY_NAME = "Test";

        public static Dictionary<Guid, CacheCleanerTester> Testers = [];

        public List<string> AccessedKeys { get; set; } = [];

        public List<string> TrackedKeys { get; set; } = [];

        public Func<List<string>, bool> RemovalCallback { get; set; }

        public bool SettingsReadCorrectly { get; set; } = false;

        public bool SettingsWritten { get; set; } = false;

        public Guid Guid { get; set; } = Guid.NewGuid();

        public CacheCleanerTester() : base()
        {
            lock (Testers)
            {
                Testers.Add(Guid, this);
            }
        }

        public override void EntryAccessed(ref CacheEntry entry, string key)
        {
            AccessedKeys.Add(key);
        }

        public override void EntryAccessed(ref CacheEntry entry, string key, long timeInTicks)
        {
            AccessedKeys.Add(key);
        }

        public override void RegisterRemovalCallback(Func<List<string>, bool> callback)
        {
            RemovalCallback = callback;
        }

        public override void TrackEntry(ref CacheEntry entry, string key)
        {
            TrackedKeys.Add(key);
        }

        public override void ReadSettings(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            _ = reader.Read();
            _ = reader.Read();
            _ = reader.Read();
            _ = reader.TryGetGuid(out var readValue);
            _ = reader.Read();

            Testers[readValue].SettingsReadCorrectly = true;
        }

        public override void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(TEST_PROPERTY_NAME);
            writer.WriteStringValue(Guid);
            writer.WriteEndObject();

            SettingsWritten = true;
        }
    }
}
