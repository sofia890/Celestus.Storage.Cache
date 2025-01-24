using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public abstract class CacheCleanerBase<KeyType>
    {
        public CacheCleanerBase()
        {

        }

        public abstract void TrackEntry(ref CacheEntry entry, KeyType key);

        public abstract void EntryAccessed(ref CacheEntry entry, KeyType key);

        public abstract void RegisterRemovalCallback(Func<List<KeyType>, bool> callback);

        public abstract void ReadSettings(ref Utf8JsonReader reader, JsonSerializerOptions options);

        public abstract void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options);
    }
}
