using Celestus.Exceptions;
using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public class CacheCleaner<IdType, KeyType>(TimeSpan interval) : CacheCleanerBase<IdType, KeyType>()
        where IdType : notnull
        where KeyType : notnull
    {
        const int DEFAULT_INTERVAL = 5000;

        long _cleanupIntervalInTicks = interval.Ticks;
        long _nextCleanupOpportunityInTicks;
        WeakReference<CacheBase<IdType, KeyType>>? _cacheReference;

        public CacheCleaner() : this(interval: TimeSpan.FromMilliseconds(DEFAULT_INTERVAL))
        {
            SetCleaningInterval(_cleanupIntervalInTicks);
        }

        private void Prune(long currentTimeInTicks)
        {
            if (_nextCleanupOpportunityInTicks > currentTimeInTicks)
            {
                return;
            }
            else if (_cacheReference == null ||
                     !_cacheReference.TryGetTarget(out var cache))
            {
                // Wait for reference to be available.
                return;
            }
            else
            {
                List<(KeyType key, CacheEntry entry)> expiredKeys = [];

                foreach (var (key, entry) in cache.Storage)
                {
                    if (ExpiredCriteria(entry, currentTimeInTicks))
                    {
                        expiredKeys.Add((key, entry));
                    }
                }

                if (expiredKeys.Count > 0)
                {
                    _ = cache.TryRemove([.. expiredKeys.Select(x => x.key)]);
                }

                _nextCleanupOpportunityInTicks = currentTimeInTicks + _cleanupIntervalInTicks;
            }
        }

        public static bool ExpiredCriteria(CacheEntry entry, long currentTimeInTicks)
        {
            return entry.Expiration <= currentTimeInTicks;
        }

        public override void EntryAccessed(ref CacheEntry entry, KeyType key)
        {
            EntryAccessed(ref entry, key, DateTime.UtcNow.Ticks);
        }

        public override void EntryAccessed(ref CacheEntry entry, KeyType key, long timeInMilliseconds)
        {
            Prune(timeInMilliseconds);
        }

        public override void RegisterCache(WeakReference<CacheBase<IdType, KeyType>> cache)
        {
            _cacheReference = cache;
        }

        public override void SetCleaningInterval(TimeSpan interval)
        {
            SetCleaningInterval(interval.Ticks);
        }

        public void SetCleaningInterval(long _intervalInTicks)
        {
            _cleanupIntervalInTicks = _intervalInTicks;
            _nextCleanupOpportunityInTicks = DateTime.UtcNow.Ticks + _cleanupIntervalInTicks;
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
            Condition.ThrowIf<MissingValueJsonException>(!intervalValueFound, nameof(_cleanupIntervalInTicks));
        }

        public override void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(_cleanupIntervalInTicks));
            writer.WriteNumberValue(_cleanupIntervalInTicks);
            writer.WriteEndObject();
        }

        public override object Clone()
        {
            var newCleaner = new CacheCleaner<IdType, KeyType>(TimeSpan.FromTicks(_cleanupIntervalInTicks));
            newCleaner.RegisterCache(_cacheReference!);

            return newCleaner;
        }
    }
}
