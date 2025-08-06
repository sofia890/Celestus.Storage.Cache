using Celestus.Serialization;
using System.Diagnostics.CodeAnalysis;
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
                    throw new StartTokenJsonException(reader.TokenType, JsonTokenType.StartObject);
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
                                    key = GetKey(ref reader);
                                    break;

                                case nameof(_storage):
                                    storage = GetStorage(ref reader, options);
                                    break;

                                case nameof(_cleaner):
                                    (cleaner, cleanerConfigured) = GetCleaner(ref reader, options);
                                    break;

                                default:
                                    reader.Skip();
                                    break;
                            }
                            break;
                    }
                }

            End:
                ValidateConfiguration(key, storage, cleaner, cleanerConfigured);

                return new Cache(key, storage, cleaner);
            }

            private string? GetKey(ref Utf8JsonReader reader)
            {
                _ = reader.Read();
                return reader.GetString();
            }

            private Dictionary<string, CacheEntry>? GetStorage(ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
                _ = reader.Read();
                return JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(ref reader, options);
            }

            private (CacheCleanerBase<string>? cleaner, bool cleanerConfigured) GetCleaner(ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
                CacheCleanerBase<string>? cleaner = null;
                bool cleanerConfigured = false;

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
                                    cleaner = CreateCleaner(ref reader, options);
                                    break;

                                case CONTENT_PROPERTY_NAME:
                                    if (cleaner == null)
                                    {
                                        throw new PropertiesOutOfOrderJsonException(TYPE_PROPERTY_NAME, CONTENT_PROPERTY_NAME);
                                    }
                                    else
                                    {
                                        cleaner.ReadSettings(ref reader, options);
                                        cleanerConfigured = true;
                                    }
                                    break;

                                default:
                                    reader.Skip();
                                    break;
                            }
                            break;
                    }
                }

            CleanerDone:
                return (cleaner, cleanerConfigured);
            }

            private CacheCleanerBase<string>? CreateCleaner(ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
                if (JsonSerializer.Deserialize<string>(ref reader, options) is not string typeString)
                {
                    throw new ValueTypeJsonException(TYPE_PROPERTY_NAME, JsonTokenType.String, reader.TokenType);
                }
                else if (Type.GetType(typeString) is not Type cleanerType)
                {
                    throw new NotObjectTypeJsonException(TYPE_PROPERTY_NAME, typeString);
                }
                else if (Activator.CreateInstance(cleanerType) is not object newObject)
                {
                    throw new ObjectCreationJsonException(TYPE_PROPERTY_NAME, cleanerType);
                }
                else if (newObject is not CacheCleanerBase<string> createdCleaner)
                {
                    throw new MissingInheritanceJsonException(TYPE_PROPERTY_NAME, newObject, typeof(CacheCleanerBase<string>));
                }
                else
                {
                    return createdCleaner;
                }
            }

            private void ValidateConfiguration([NotNull] string? key,
                                               [NotNull] Dictionary<string, CacheEntry>? storage,
                                               [NotNull] CacheCleanerBase<string>? cleaner,
                                               bool cleanerConfigured)
            {
                if (key == null)
                {
                    throw new MissingValueJsonException(nameof(Key));
                }
                else if (storage == null)
                {
                    throw new MissingValueJsonException(nameof(_storage));
                }
                else if (cleaner == null)
                {
                    throw new MissingValueJsonException(nameof(_cleaner));
                }
                else if (!cleanerConfigured)
                {
                    throw new MissingValueJsonException(CONTENT_PROPERTY_NAME);
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
        readonly static Dictionary<string, WeakReference<Cache>> _caches = [];
        readonly static CacheFactoryCleaner<Cache> _factoryCleaner = new(_caches);

        public static bool IsLoaded(string key, out Cache? cache)
        {
            cache = null;

            return _caches.TryGetValue(key, out var cacheReference) &&
                   cacheReference.TryGetTarget(out cache);
        }

        public static Cache GetOrCreateShared(string key = "")
        {
            var usedKey = (key.Length > 0) ? key : Guid.NewGuid().ToString();

            lock (nameof(Cache))
            {
                if (_caches.TryGetValue(usedKey, out var cacheReference) &&
                    cacheReference.TryGetTarget(out var cache))
                {
                    return cache;
                }
                else
                {
                    cache = new Cache(usedKey);

                    _caches[usedKey] = new(cache);

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
            else if (IsLoaded(loadedCache.Key, out var cache) &&
                     cache != null)
            {
                lock (nameof(Cache))
                {
                    var cacheReference = _caches[loadedCache.Key];

                    cache._storage = loadedCache._storage;

                    return cache;
                }
            }
            else
            {
                lock (nameof(Cache))
                {
                    _caches[loadedCache.Key] = new(loadedCache);
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
            Dictionary<string, CacheEntry> storage,
            CacheCleanerBase<string> cleaner,
            bool removalRegistered = false)
        {
            _storage = storage;
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
            var currentTimeInTicks = DateTime.UtcNow.Ticks;

            if (!_storage.TryGetValue(key, out var entry))
            {
                return (false, default);
            }
            else if (entry.Expiration < currentTimeInTicks)
            {
                _cleaner.EntryAccessed(ref entry, key, currentTimeInTicks);

                return (false, default);
            }
            else if (entry.Data is not DataType data)
            {
                _cleaner.EntryAccessed(ref entry, key);

                return (false, default);
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
            if (other == null || _storage.Count != other._storage.Count)
            {
                return false;
            }

            // Compare each key-value pair efficiently
            foreach (var kvp in _storage)
            {
                if (!other._storage.TryGetValue(kvp.Key, out var otherValue) || 
                    !kvp.Value.Equals(otherValue))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Cache);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            
            // Sort keys to ensure consistent hash code regardless of insertion order
            foreach (var kvp in _storage.OrderBy(x => x.Key))
            {
                hash.Add(kvp.Key);
                hash.Add(kvp.Value);
            }
            
            return hash.ToHashCode();
        }
        #endregion
    }
}
