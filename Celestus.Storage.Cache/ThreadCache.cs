using System;
using System.Runtime.Serialization;

namespace Celestus.Storage.Cache
{
    [Serializable]
    public class ThreadCache : IDisposable, ISerializable, IEquatable<ThreadCache>
    {
        #region Factory Pattern
        readonly static Dictionary<string, ThreadCache> _caches = [];

        private static void RemoveDisposedCache(object? sender, string key)
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
                    if (_caches.TryGetValue(usedKey, out var cache) && !cache.IsDisposed)
                    {
                        return cache;
                    }
                    else
                    {
                        cache = new ThreadCache(usedKey);
                        cache.OnDisposed += RemoveDisposedCache;

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

        readonly ReaderWriterLockSlim _lock = new();

        protected Cache _cache;

        public bool IsDisposed { get; set; } = false;
        private event EventHandler<string>? OnDisposed;

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
            if (_lock.TryEnterWriteLock(timeout))
            {
                try
                {
                    _cache.Set(key, value, duration);

                    return true;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            else
            {
                return false;
            }
        }
        public (bool result, DataType? data) TryGet<DataType>(string key, int timeout = -1)
        {
            if (!_lock.TryEnterReadLock(timeout))
            {
                return (false, default);
            }
            else
            {
                try
                {
                    return _cache.TryGet<DataType>(key);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                OnDisposed?.Invoke(this, _key);

                if (disposing)
                {
                    _lock.Dispose();
                }

                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region ISerializable
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Cache", _cache);
            info.AddValue("Key", _key);
            info.AddValue("ones", (1L, typeof(long)), typeof(long));
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
        #endregion
    }
}
