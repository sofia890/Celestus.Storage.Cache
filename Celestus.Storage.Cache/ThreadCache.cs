﻿using Celestus.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class CacheLock : IDisposable
    {
        private readonly ReaderWriterLock _lock;
        public CacheLock(ReaderWriterLock cacheLock, int timeout = -1)
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
                        case JsonTokenType.EndObject:
                            break;

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

        public static ThreadCache? UpdateOrLoadSharedFromFile(Uri path, int timeout = -1)
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

        readonly ReaderWriterLock _lock = new();

        Cache _cache = cache;

        public string Key { get; init; } = key;

        public ThreadCache(string key) : this(key, new())
        {
        }

        public ThreadCache() : this(string.Empty, new())
        {
        }

        public CacheLock Lock(int timeout = -1)
        {
            return new CacheLock(_lock, timeout);
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null, int timeout = -1)
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
        public (bool result, DataType? data) TryGet<DataType>(string key, int timeout = -1)
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

        public void SaveToFile(Uri path)
        {
            Serialize.SaveToFile(this, path);
        }

        public static ThreadCache? TryCreateFromFile(Uri path)
        {
            return Serialize.TryCreateFromFile<ThreadCache>(path);
        }

        public bool TryLoadFromFile(Uri path)
        {
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
