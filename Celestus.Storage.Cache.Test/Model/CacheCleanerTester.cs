using System.Text.Json;

namespace Celestus.Storage.Cache.Test.Model
{
    internal class CacheCleanerTester : CacheCleanerBase<string>
    {
        const string TEST_PROPERTY_NAME = "Test";

        public static Dictionary<Guid, CacheCleanerTester> Testers = [];

        public List<string> AccessedKeys { get; set; } = [];

        public WeakReference<IEnumerable<KeyValuePair<string, CacheEntry>>> StorageCollection { get; set; } = new([]);

        public bool SettingsReadCorrectly { get; set; } = false;

        public bool SettingsWritten { get; set; } = false;

        public Guid Guid { get; set; } = Guid.NewGuid();

        WeakReference<CacheBase<string>>? _cacheReference;

        public CacheCleanerTester() : base()
        {
            lock (Testers)
            {
                Testers.Add(Guid, this);
            }
        }

        public override void EntryAccessed(ref CacheEntry entry, string key)
        {
            if (IsDisposed)
            {
                return;
            }

            AccessedKeys.Add(key);
        }

        public override void EntryAccessed(ref CacheEntry entry, string key, long timeInTicks)
        {
            if (IsDisposed)
            {
                return;
            }

            AccessedKeys.Add(key);
        }

        public override void RegisterCache(WeakReference<CacheBase<string>> cache)
        {
            if (IsDisposed)
            {
                return;
            }

            _cacheReference = cache;
        }

        public override void SetCleaningInterval(TimeSpan interval)
        {
            throw new NotImplementedException();
        }

        public override void ReadSettings(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _ = reader.Read();
            _ = reader.Read();
            _ = reader.Read();
            _ = reader.TryGetGuid(out var readValue);
            _ = reader.Read();

            lock (Testers)
            {
                if (Testers.TryGetValue(readValue, out var tester))
                {
                    tester.SettingsReadCorrectly = true;
                }
            }
        }

        public override void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            writer.WriteStartObject();
            writer.WritePropertyName(TEST_PROPERTY_NAME);
            writer.WriteStringValue(Guid);
            writer.WriteEndObject();

            SettingsWritten = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (Testers)
                {
                    Testers.Remove(Guid);
                }

                // Clear collections to help with garbage collection
                AccessedKeys.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
