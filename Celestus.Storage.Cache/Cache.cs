using Celestus.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(CacheJsonConverter))]
    public class Cache(string key, Dictionary<string, CacheEntry> storge)
    {

        public class CacheJsonConverter : JsonConverter<Cache>
        {
            public override Cache Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                }

                string? key = null;
                Dictionary<string, CacheEntry>? storage = null;

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

                                default:
                                    throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                            }

                            break;
                    }
                }

            End:
                if (key == null || storage == null)
                {
                    throw new JsonException($"Invalid JSON for {nameof(Cache)}.");
                }

                return new Cache(key, storage);
            }

            public override void Write(Utf8JsonWriter writer, Cache value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WriteString(nameof(Key), value.Key);

                writer.WritePropertyName(nameof(_storage));
                JsonSerializer.Serialize(writer, value._storage, options);

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

        Dictionary<string, CacheEntry> _storage = storge;

        public string Key { get; init; } = key;

        public Cache(string key) : this(key, [])
        {

        }

        public Cache() : this(string.Empty, [])
        {

        }

        public Cache(Cache other) : this(other.Key, new(other._storage))
        {

        }

        public void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            long expiration = long.MaxValue;

            if (duration != null && duration != TimeSpan.Zero)
            {
                expiration = DateTime.Now.Ticks + (duration?.Ticks ?? 0);
            }

            Set(key, value, expiration);
        }

        public void Set<DataType>(string key, DataType value, long expiration)
        {
            _storage[key] = new(expiration, value);
        }

        public (bool result, DataType? data) TryGet<DataType>(string key)
        {
            if (!_storage.TryGetValue(key, out var entry))
            {
                return (false, default);
            }
            else if (entry.Expiration < DateTime.Now.Ticks)
            {
                return (false, default);
            }
            else if (entry.Data is not DataType data)
            {
                return (false, default);
            }
            else
            {
                return (true, data);
            }
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
