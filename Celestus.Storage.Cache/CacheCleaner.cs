
using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public class CacheCleaner<KeyType>(int cleanupIntervalInMs) : CacheCleanerBase<KeyType>()
    {
        const int DEFAULT_INTERVAL = 5000;

        readonly List<(KeyType key, CacheEntry entry)> _entries = [];
        long _cleanupIntervalInTicks = TimeSpan.FromMilliseconds(cleanupIntervalInMs).Ticks;
        long _nextCleanupOpportunityInTicks = 0;
        Func<List<KeyType>, bool> _removalCallback = (key) => false;

        public CacheCleaner() : this(cleanupIntervalInMs: DEFAULT_INTERVAL)
        {
        }

        private void Prune()
        {
            var currentTimeInTicks = DateTime.UtcNow.Ticks;

            if (_entries.Count > 0 && _nextCleanupOpportunityInTicks <= currentTimeInTicks)
            {
                List<(KeyType key, CacheEntry entry)> expiredKeys = [];

                foreach (var (key, entry) in _entries)
                {
                    if (ExpiredCriteria(entry, currentTimeInTicks))
                    {
                        expiredKeys.Add((key, entry));
                    }
                }

                foreach (var element in expiredKeys)
                {
                    _entries.Remove(element);
                }

                if (expiredKeys.Count > 0)
                {
                    _ = _removalCallback([.. expiredKeys.Select(x => x.key)]);
                }

                _nextCleanupOpportunityInTicks = currentTimeInTicks + _cleanupIntervalInTicks;
            }
        }

        public static bool ExpiredCriteria(CacheEntry entry, long currentTimeInTicks)
        {
            return entry.Expiration <= currentTimeInTicks;
        }

        public override void TrackEntry(ref CacheEntry entry, KeyType key)
        {
            Prune();

            _entries.Add((key, entry));
        }

        public override void EntryAccessed(ref CacheEntry entry, KeyType key)
        {
            Prune();
        }

        public override void RegisterRemovalCallback(Func<List<KeyType>, bool> callback)
        {
            _removalCallback = callback;
        }

        public override void ReadSettings(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            bool intervalValueFound = false;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    default:
                        break;

                    case JsonTokenType.EndObject:
                        goto End;

                    case JsonTokenType.StartObject:
                        break;

                    case JsonTokenType.PropertyName:
                        switch (reader.GetString())
                        {
                            case nameof(_cleanupIntervalInTicks):
                                _ = reader.Read();

                                _cleanupIntervalInTicks = reader.GetInt64();
                                _nextCleanupOpportunityInTicks = 0;
                                intervalValueFound = true;
                                break;

                            default:
                                break;

                        }
                        break;
                }
            }

        End:
            if (!intervalValueFound)
            {
                throw new MissingValueJsonException(nameof(_cleanupIntervalInTicks));
            }
        }

        public override void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(_cleanupIntervalInTicks));
            writer.WriteNumberValue(_cleanupIntervalInTicks);
            writer.WriteEndObject();
        }
    }
}
