using System.Runtime.Serialization;

namespace Celestus.Storage.Cache
{
    [Serializable]
    public class ThreadCache : ISerializable, IEquatable<ThreadCache>
    {
        #region Factory Pattern
        readonly static Dictionary<string, ThreadCache> _caches = [];

        private static void RemoveDisposedCache(object? _, string key)
        {
            lock (nameof(ThreadCache))
            {
                _ = _caches.Remove(key);
            }
        }

        public static ThreadCache CreateShared(string key = "", bool tracked = true)
        {
            var usedKey = (key.Length > 0) ? key : Guid.NewGuid().ToString();

            if (tracked)
            {
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
            else
            {
                return new ThreadCache(usedKey);
            }

        }
        #endregion

        readonly ReaderWriterLock _lock = new();

        protected Cache _cache;

        readonly protected string _key;

        private ThreadCache(string key)
        {
            _cache = new Cache();

            _key = key;
        }

        private ThreadCache()
        {
            _cache = new Cache();

            _key = string.Empty;
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

        #region ISerializable
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Cache", _cache);
            info.AddValue("Key", _key);
        }

        protected ThreadCache(SerializationInfo info, StreamingContext context)
        {
            var cacheObject = info.GetValue("Cache", typeof(Cache));

            if (cacheObject is Cache cache)
            {
                _cache = cache;
            }
            else
            {
                throw new SerializationException($"Cache is not a {typeof(Cache)}.");
            }

            _key = info.GetString("Key") ?? throw new SerializationException("Key cannot be null");
        }
        #endregion

        #region IEquatable
        public bool Equals(ThreadCache? other)
        {
            return other != null &&
                _cache.Equals(other._cache) &&
                _key.Equals(other._key);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ThreadCache);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_cache, _key);
        }
        #endregion
    }
}
