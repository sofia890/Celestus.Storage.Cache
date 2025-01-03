using System.Runtime.Serialization;

namespace Celestus.Storage.Cache
{
    public class ThreadCache : IDisposable, ISerializable
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

        public static ThreadCache CreateShared(string key, int timeout)
        {
            lock (nameof(ThreadCache))
            {
                if (_caches.TryGetValue(key, out var cache) && !cache.IsDisposed)
                {
                    return cache;
                }
                else
                {
                    cache = new ThreadCache(key, timeout);
                    cache.OnDisposed += RemoveDisposedCache;

                    _caches[key] = cache;

                    return cache;
                }
            }
        }
        #endregion
        private record CacheEntry(DateTime Expiration, object? Data);

        readonly ReaderWriterLockSlim _lock = new();
        readonly Dictionary<string, CacheEntry> _storage = [];

        public bool IsDisposed { get; set; } = false;
        private event EventHandler<string>? OnDisposed;

        readonly private string Key;
        readonly private int Timeout;

        private ThreadCache(string key, int timeout)
        {
            Key = key;
            Timeout = timeout;

            if (Timeout == 0)
            {
                throw new ArgumentException($"{nameof(timeout)} cannot be zero.");
            }
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            if (_lock.TryEnterWriteLock(Timeout))
            {
                try
                {
                    DateTime expiratonDate = DateTime.MaxValue;

                    if (duration != null && duration != TimeSpan.Zero)
                    {
                        _ = DateTime.Now.Add((TimeSpan)duration);
                    }

                    _storage[key] = new(expiratonDate, value);

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
        public (bool result, DataType? data) TryGet<DataType>(string key)
        {
            if (!_lock.TryEnterReadLock(Timeout))
            {
                return (false, default);
            }
            else
            {
                try
                {
                    if (!_storage.TryGetValue(key, out var entry))
                    {
                        return (false, default);
                    }
                    else if (entry.Expiration < DateTime.Now)
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
                OnDisposed?.Invoke(this, Key);

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
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Key", Key);
            info.AddValue("Timeout", Timeout);
            info.AddValue("Storage", _storage);
        }

        protected ThreadCache(SerializationInfo info, StreamingContext context)
        {
            Key = info.GetString("Key") ?? throw new SerializationException("Key cannot be null");

            Timeout = info.GetInt32("Timeout");

            if (Timeout == 0)
            {
                throw new SerializationException($"{nameof(Timeout)} cannot be 0.");
            }

            if (info.GetValue("Storage", typeof(Dictionary<string, CacheEntry>)) is Dictionary<string, CacheEntry> storage)
            {
                _storage = storage;
            }
            else
            {
                throw new SerializationException($"Storage is not a {nameof(Dictionary<string, CacheEntry>)}.");
            }
        }
        #endregion
    }
}
