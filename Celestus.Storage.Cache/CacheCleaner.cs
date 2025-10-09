using Celestus.Exceptions;
using System.Text.Json;

namespace Celestus.Storage.Cache
{
    /// <summary>
    /// Single threaded cache cleaner.
    /// </summary>
    public class CacheCleaner<CacheIdType, CacheKeyType>(TimeSpan interval) : CacheCleanerBase<CacheIdType, CacheKeyType>()
        where CacheIdType : notnull
        where CacheKeyType : notnull
    {
        const int DEFAULT_INTERVAL = 5000;

        TimeSpan _cleanupInterval = interval;
        DateTime _nextCleanupOpportunity;
        WeakReference<CacheBase<CacheIdType, CacheKeyType>>? _cacheReference;

        public CacheCleaner() : this(interval: TimeSpan.FromMilliseconds(DEFAULT_INTERVAL))
        {
            SetCleaningInterval(_cleanupInterval);
        }

        private void Prune(DateTime now)
        {
            if (_nextCleanupOpportunity > now)
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
                List<(CacheKeyType key, CacheEntry entry)> expiredKeys = [];

                foreach (var (key, entry) in cache.Storage)
                {
                    if (ExpiredCriteria(entry, now))
                    {
                        expiredKeys.Add((key, entry));
                    }
                }

                if (expiredKeys.Count > 0)
                {
                    _ = cache.TryRemove([.. expiredKeys.Select(x => x.key)]);
                }

                _nextCleanupOpportunity = now + _cleanupInterval;
            }
        }

        public static bool ExpiredCriteria(CacheEntry entry, DateTime now)
        {
            return entry.Expiration <= now;
        }

        public override void EntryAccessed(ref CacheEntry entry, CacheKeyType key)
        {
            EntryAccessed(ref entry, key, DateTime.UtcNow);
        }

        public override void EntryAccessed(ref CacheEntry entry, CacheKeyType key, DateTime when)
        {
            Prune(when);
        }

        public override void RegisterCache(WeakReference<CacheBase<CacheIdType, CacheKeyType>> cache)
        {
            _cacheReference = cache;
        }

        public override void UnregisterCache()
        {
            _cacheReference = null;
        }

        public override void SetCleaningInterval(TimeSpan interval)
        {
            _cleanupInterval = interval;
            _nextCleanupOpportunity = DateTime.UtcNow + _cleanupInterval;
        }

        public override void ReadSettings(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            bool intervalValueFound = false;

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        goto End;
                    case JsonTokenType.PropertyName:
                        var propertyName = reader.GetString();
                        _ = reader.Read();

                        switch (propertyName)
                        {
                            case nameof(_cleanupInterval):
                                _cleanupInterval = JsonSerializer.Deserialize<TimeSpan>(ref reader, options);
                                intervalValueFound = true;
                                break;
                            default:
                                reader.Skip();
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
        End:
            Condition.ThrowIf<MissingValueJsonException>(!intervalValueFound, $"Invalid JSON for {nameof(CacheCleaner<CacheIdType, CacheKeyType>)}");
        }

        public override void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(nameof(_cleanupInterval));
            JsonSerializer.Serialize(writer, _cleanupInterval, options);
            writer.WriteEndObject();
        }

        public override object Clone()
        {
            return new CacheCleaner<CacheIdType, CacheKeyType>(TimeSpan.FromTicks(_cleanupInterval.Ticks));
        }
    }
}
