using Celestus.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(CacheJsonConverter))]
    public class Cache
    {
        public class CacheJsonConverter : JsonConverter<Cache>
        {
            const string TYPE_PROPERTY_NAME = "Type";
            const string CONTENT_PROPERTY_NAME = "Content";

            public override Cache Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                }

                string? key = null;
                Dictionary<string, CacheEntry>? storage = null;
                CacheCleanerBase<string>? cleaner = null;
                bool cleanerConfigured = false;

                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        default:
                        case JsonTokenType.EndObject:
                            goto End;

                        case JsonTokenType.PropertyName:
                            switch (reader.GetString())
                            {
                                case nameof(Key):
                                    _ = reader.Read();

                                    key = reader.GetString();
                                    break;

                                case nameof(_storage):
                                    _ = reader.Read();

                                    storage = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(ref reader, options);
                                    break;

                                case nameof(_cleaner):
                                    while (reader.Read())
                                    {
                                        switch (reader.TokenType)
                                        {
                                            default:
                                            case JsonTokenType.EndObject:
                                                goto CleanerDone;

                                            case JsonTokenType.StartObject:
                                                break;

                                            case JsonTokenType.PropertyName:
                                                switch (reader.GetString())
                                                {
                                                    case TYPE_PROPERTY_NAME:
                                                        if (JsonSerializer.Deserialize<string>(ref reader, options) is not string typeString)
                                                        {
                                                            throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                                                        }
                                                        else if (Type.GetType(typeString) is not Type cleanerType)
                                                        {
                                                            throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                                                        }
                                                        else if (Activator.CreateInstance(cleanerType) is not CacheCleanerBase<string> createdCleaner)
                                                        {
                                                            throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                                                        }
                                                        else
                                                        {
                                                            cleaner = createdCleaner;
                                                        }
                                                        break;

                                                    case CONTENT_PROPERTY_NAME:
                                                        if (cleaner == null)
                                                        {
                                                            throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                                                        }
                                                        else
                                                        {
                                                            cleaner.ReadSettings(ref reader, options);
                                                            cleanerConfigured = true;
                                                        }
                                                        break;

                                                    default:
                                                        _ = reader.Read();
                                                        break;
                                                }
                                                break;
                                        }
                                    }

                                CleanerDone:
                                    break;

                                default:
                                    throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                            }

                            break;
                    }
                }

            End:
                if (key == null || storage == null || cleaner == null || !cleanerConfigured)
                {
                    throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                }
                else
                {
                    return new Cache(key, storage, cleaner);
                }
            }

            public override void Write(Utf8JsonWriter writer, Cache value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WriteString(nameof(Key), value.Key);

                writer.WritePropertyName(nameof(_storage));
                JsonSerializer.Serialize(writer, value._storage, options);

                writer.WritePropertyName(nameof(_cleaner));
                writer.WriteStartObject();

                var type = value._cleaner.GetType();
                writer.WritePropertyName(TYPE_PROPERTY_NAME);
                writer.WriteStringValue(type.AssemblyQualifiedName);

                writer.WritePropertyName(CONTENT_PROPERTY_NAME);
                value._cleaner.WriteSettings(writer, options);
                writer.WriteEndObject();

                writer.WriteEndObject();
            }
        }

        #region Factory Pattern
        readonly static Dictionary<string, Cache> _caches = [];

        public static bool IsLoaded(string key)
        {
            return _caches.ContainsKey(key);
        }

        public static Cache GetOrCreateShared(string key = "")
        {
            var usedKey = (key.Length > 0) ? key : Guid.NewGuid().ToString();

            lock (nameof(Cache))
            {
                if (_caches.TryGetValue(usedKey, out var cache))
                {
                    return cache;
                }
                else
                {
                    cache = new Cache(usedKey);

                    _caches[usedKey] = cache;

                    return cache;
                }
            }
        }

        public static Cache? UpdateOrLoadSharedFromFile(Uri path)
        {
            if (TryCreateFromFile(path) is not Cache loadedCache)
            {
                return null;
            }
            else if (IsLoaded(loadedCache.Key))
            {
                lock (nameof(Cache))
                {
                    var cache = _caches[loadedCache.Key];

                    cache._storage = loadedCache._storage;

                    return cache;
                }
            }
            else
            {
                lock (nameof(ThreadCache))
                {
                    _caches[loadedCache.Key] = loadedCache;
                }

                return loadedCache;
            }
        }
        #endregion

        Dictionary<string, CacheEntry> _storage;
        readonly CacheCleanerBase<string> _cleaner;

        public string Key { get; init; }

        private Cache(
            string key,
            Dictionary<string, CacheEntry> storge,
            CacheCleanerBase<string> cleaner,
            bool removalRegistered = false)
        {
            _storage = storge;
            _cleaner = cleaner;

            if (!removalRegistered)
            {
                _cleaner.RegisterRemovalCallback(TryRemove);
            }

            Key = key;
        }

        public Cache(string key) : this(key, [], new CacheCleaner<string>())
        {
        }

        public Cache() : this(string.Empty)
        {
        }

        public Cache(CacheCleanerBase<string> cleaner, bool doNotSetRemoval = false) :
            this(string.Empty, [], cleaner, doNotSetRemoval)
        {
        }

        public void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            long expiration = long.MaxValue;

            if (duration is TimeSpan timeDuration)
            {
                expiration = DateTime.UtcNow.Ticks + timeDuration.Ticks;
            }

            Set(key, value, expiration);
        }

        public void Set<DataType>(string key, DataType value, long expiration)
        {
            var entry = new CacheEntry(expiration, value);
            _storage[key] = entry;

            _cleaner.TrackEntry(ref entry, key);
        }

        public (bool result, DataType? data) TryGet<DataType>(string key)
        {
            if (!_storage.TryGetValue(key, out var entry))
            {
                return (false, default);
            }
            else if (entry.Expiration < DateTime.UtcNow.Ticks)
            {
                _cleaner.EntryAccessed(ref entry, key);

                return (false, default);
            }
            else if (entry.Data is not DataType data)
            {
                _cleaner.EntryAccessed(ref entry, key);

                return (entry.Data == null, default);
            }
            else
            {
                _cleaner.EntryAccessed(ref entry, key);

                return (true, data);
            }
        }

        public bool TryRemove(List<string> keys)
        {
            bool anyRemoved = false;

            for (int i = 0; i < keys.Count; i++)
            {
                anyRemoved |= _storage.Remove(keys[i]);
            }

            return anyRemoved;
        }

        public void SaveToFile(Uri path)
        {
            Serialize.SaveToFile(this, path);
        }

        public static Cache? TryCreateFromFile(Uri path)
        {
            return Serialize.TryCreateFromFile<Cache>(path);
        }

        public bool TryLoadFromFile(Uri path)
        {
            var loadedData = Serialize.TryCreateFromFile<Cache>(path);

            if (loadedData == null)
            {
                return false;
            }
            else
            {
                _storage = loadedData._storage;

                return true;
            }
        }

        #region IEquatable
        public bool Equals(Cache? other)
        {
            return other != null &&
                   _storage.Count == other._storage.Count &&
                   _storage.Intersect(other._storage).Count() == _storage.Count;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Cache);
        }

        public override int GetHashCode()
        {
            return _storage.Aggregate(0, (a, b) => HashCode.Combine(a, b));
        }
        #endregion
    }
}
