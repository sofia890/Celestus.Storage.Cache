using Celestus.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class CacheLock : IDisposable
    {
        private readonly ReaderWriterLock _lock;
        public CacheLock(ReaderWriterLock cacheLock, int timeout = ThreadCache.NO_TIMEOUT)
        {
            _lock = cacheLock;
            _lock.AcquireWriterLock(timeout);
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (_lock.IsWriterLockHeld)
            {
                _lock.ReleaseWriterLock();
            }
        }
    }

    [JsonConverter(typeof(ThreadCacheJsonConverter))]
    public class ThreadCache(string key, Cache cache)
    {
        public class ThreadCacheJsonConverter : JsonConverter<ThreadCache>
        {
            const int DEFAULT_LOCK_TIMEOUT = 10000;

            public override ThreadCache? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException($"Invalid JSON for {nameof(ThreadCache)}.");
                }

                string? key = null;
                Cache? cache = null;

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

                                case nameof(_cache):
                                    _ = reader.Read();

                                    cache = JsonSerializer.Deserialize<Cache>(ref reader, options);
                                    break;

                                default:
                                    throw new JsonException($"Invalid JSON for {nameof(ThreadCache)}.");
                            }

                            break;
                    }
                }

            End:
                if (key == null || cache == null)
                {
                    throw new JsonException($"Invalid JSON for {nameof(ThreadCache)}.");
                }

                return new ThreadCache(key, cache);
            }

            public override void Write(Utf8JsonWriter writer, ThreadCache value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WriteString(nameof(Key), value.Key);
                writer.WritePropertyName(nameof(_cache));

                using (value.Lock(DEFAULT_LOCK_TIMEOUT))
                {
                    JsonSerializer.Serialize(writer, value._cache, options);
                }

                writer.WriteEndObject();
            }
        }

        #region Factory Pattern
        readonly static Dictionary<string, ThreadCache> _caches = [];

        public static bool IsLoaded(string key)
        {
            return _caches.ContainsKey(key);
        }

        public static ThreadCache GetOrCreateShared(string key = "")
        {
            var usedKey = (key.Length > 0) ? key : Guid.NewGuid().ToString();

            lock (nameof(ThreadCache))
            {
                if (_caches.TryGetValue(usedKey, out var cache))
                {
                    return cache;
                }
                else
                {
                    cache = new ThreadCache(usedKey);

                    _caches[usedKey] = cache;

                    return cache;
                }
            }
        }

        public static ThreadCache? UpdateOrLoadSharedFromFile(Uri path, int timeout = NO_TIMEOUT)
        {
            if (TryCreateFromFile(path) is not ThreadCache loadedCache)
            {
                return null;
            }
            else if (IsLoaded(loadedCache.Key))
            {
                lock (nameof(ThreadCache))
                {
                    var threadCache = _caches[loadedCache.Key];

                    try
                    {
                        threadCache._lock.AcquireWriterLock(timeout);
                        threadCache._cache = loadedCache._cache;

                        return threadCache;
                    }
                    catch (ApplicationException)
                    {
                        return null;
                    }
                    finally
                    {
                        if (threadCache._lock.IsWriterLockHeld)
                        {
                            threadCache._lock.ReleaseWriterLock();
                        }
                    }
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

        const int CLEANER_INTERVAL_IN_MS = 5000;
        public const int NO_TIMEOUT = -1;

        readonly ReaderWriterLock _lock = new();

        Cache _cache = cache;

        public string Key { get; init; } = key;

        public ThreadCache(string key, CacheCleanerBase<string> cleaner) :
            this(key, new Cache(cleaner))
        {
            cleaner.RegisterRemovalCallback(Remove);
        }
        public ThreadCache(CacheCleanerBase<string> cleaner) :
            this(string.Empty, new Cache(cleaner))
        {
        }

        public ThreadCache(string key, int cleaningIntervalInMs = CLEANER_INTERVAL_IN_MS) :
            this(key, new ThreadCacheCleaner<string>(cleaningIntervalInMs))
        {
        }

        public ThreadCache(int cleaningIntervalInMs = CLEANER_INTERVAL_IN_MS) :
            this(string.Empty, new ThreadCacheCleaner<string>(cleaningIntervalInMs))
        {
        }

        public CacheLock Lock(int timeout = NO_TIMEOUT)
        {
            return new CacheLock(_lock, timeout);
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null, int timeout = NO_TIMEOUT)
        {
            try
            {
                _lock.AcquireWriterLock(timeout);

                _cache.Set(key, value, duration);

                return true;
            }
            catch (ApplicationException)
            {
                return false;
            }
            finally
            {
                if (_lock.IsWriterLockHeld)
                {
                    _lock.ReleaseWriterLock();
                }
            }
        }
        public (bool result, DataType? data) TryGet<DataType>(string key, int timeout = NO_TIMEOUT)
        {
            try
            {
                _lock.AcquireReaderLock(timeout);

                return _cache.TryGet<DataType>(key);
            }
            catch (ApplicationException)
            {
                return (false, default);
            }
            finally
            {
                if (_lock.IsReaderLockHeld)
                {
                    _lock.ReleaseReaderLock();
                }
            }
        }

        public bool Remove(List<string> keys)
        {
            return Remove(keys, timeout: NO_TIMEOUT);
        }

        public bool Remove(List<string> keys, int timeout = NO_TIMEOUT)
        {
            try
            {
                _lock.AcquireWriterLock(timeout);

                return _cache.Remove(keys);
            }
            catch (ApplicationException)
            {
                return false;
            }
            finally
            {
                if (_lock.IsWriterLockHeld)
                {
                    _lock.ReleaseWriterLock();
                }
            }
        }

        public void SaveToFile(Uri path)
        {
            using var _ = Lock();

            Serialize.SaveToFile(this, path);
        }

        public static ThreadCache? TryCreateFromFile(Uri path)
        {
            return Serialize.TryCreateFromFile<ThreadCache>(path);
        }

        public bool TryLoadFromFile(Uri path)
        {
            using var _ = Lock();

            var loadedData = Serialize.TryCreateFromFile<ThreadCache>(path);

            if (loadedData == null)
            {
                return false;
            }
            else if (Key != loadedData.Key)
            {
                return false;
            }
            else
            {
                _cache = loadedData._cache;

                return true;
            }
        }

        #region IEquatable
        public bool Equals(ThreadCache? other)
        {
            return other != null &&
                   _cache.Equals(other._cache) &&
                   Key.Equals(other.Key);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ThreadCache);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_cache, Key);
        }
        #endregion
    }
}
