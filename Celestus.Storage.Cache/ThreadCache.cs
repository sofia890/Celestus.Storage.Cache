using Celestus.Serialization;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(ThreadCacheJsonConverter))]
    public class ThreadCache(string key, Cache cache)
    {
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

        internal Cache _cache = cache;

        public string Key { get; init; } = key;

        public ThreadCache(string key) : this(key, new())
        {
        }

        public ThreadCache() : this(string.Empty, new())
        {
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
