
using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public class CacheCleaner<KeyType>(TimeSpan interval) : CacheCleanerBase<KeyType>()
        where KeyType : notnull
    {
        const int DEFAULT_INTERVAL = 5000;

        WeakReference<IEnumerable<KeyValuePair<KeyType, CacheEntry>>> _collectionReference = new(new Dictionary<KeyType, CacheEntry>());

        long _cleanupIntervalInTicks = interval.Ticks;
        long _nextCleanupOpportunityInTicks = 0;
        WeakReference<Func<List<KeyType>, bool>>? _removalCallbackReference;

        public CacheCleaner() : this(interval: TimeSpan.FromMilliseconds(DEFAULT_INTERVAL))
        {
        }

        private void Prune(long currentTimeInTicks)
        {
            if (_nextCleanupOpportunityInTicks > currentTimeInTicks)
            {
                return;
            }
            else if (!_collectionReference.TryGetTarget(out var collection))
            {
                // Wait for reference to be available.
                return;
            }
            else
            {
                List<(KeyType key, CacheEntry entry)> expiredKeys = [];

                foreach (var (key, entry) in collection)
                {
                    if (ExpiredCriteria(entry, currentTimeInTicks))
                    {
                        expiredKeys.Add((key, entry));
                    }
                }

                if (expiredKeys.Count > 0 && (_removalCallbackReference?.TryGetTarget(out var callback) ?? false))
                {
                    _ = callback([.. expiredKeys.Select(x => x.key)]);
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
            Prune(DateTime.UtcNow.Ticks);
        }

        public override void EntryAccessed(ref CacheEntry entry, KeyType key)
        {
            EntryAccessed(ref entry, key, DateTime.UtcNow.Ticks);
        }

        public override void EntryAccessed(ref CacheEntry entry, KeyType key, long timeInMilliseconds)
        {
            Prune(timeInMilliseconds);
        }

        public override void RegisterRemovalCallback(WeakReference<Func<List<KeyType>, bool>> callback)
        {
            _removalCallbackReference = callback;
        }

        public override void RegisterCollection(WeakReference<IEnumerable<KeyValuePair<KeyType, CacheEntry>>> collection)
        {
            _collectionReference = collection;
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
