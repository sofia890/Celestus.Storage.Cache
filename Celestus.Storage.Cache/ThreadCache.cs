using Celestus.Serialization;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    public class CacheLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;

        public CacheLock(ReaderWriterLockSlim cacheLock, int timeout = ThreadCache.NO_TIMEOUT)
        {
            _lock = cacheLock;
            
            if (!_lock.TryEnterWriteLock(timeout))
            {
                throw new TimeoutException("Timeout while attempting to acquire a write lock.");
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (_lock.IsWriteLockHeld)
            {
                _lock.ExitWriteLock();
            }
        }
    }

    [JsonConverter(typeof(ThreadCacheJsonConverter))]
    public class ThreadCache : IDisposable
    {
        const int CLEANER_INTERVAL_IN_MS = 5000;
        public const int NO_TIMEOUT = -1;

        private bool _disposed = false;

        internal CacheCleanerBase<string>? Cleaner { get; private set; } = null;

        internal Cache Cache { get; set; }

        ReaderWriterLockSlim _lock = new();

        public string Key { get; init; }

        public ThreadCache(string key, Cache cache, CacheCleanerBase<string>? cleaner = null)
        {
            Key = key;
            Cache = cache;
            Cleaner = cleaner;
        }

        public ThreadCache(string key, CacheCleanerBase<string> cleaner) :
            this(key, new Cache(cleaner, doNotSetRemoval: true), cleaner)
        {
            cleaner.RegisterRemovalCallback(TryRemove);
        }

        public ThreadCache(CacheCleanerBase<string> cleaner) :
            this(string.Empty, cleaner)
        {
        }

        public ThreadCache(string key, int cleaningIntervalInMs = CLEANER_INTERVAL_IN_MS) :
            this(key, cleaner: new ThreadCacheCleaner<string>(cleaningIntervalInMs))
        {
        }

        public ThreadCache(int cleaningIntervalInMs = CLEANER_INTERVAL_IN_MS) :
            this(string.Empty, cleaningIntervalInMs)
        {
        }

        public CacheLock ThreadLock(int timeout = NO_TIMEOUT)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            return new CacheLock(_lock, timeout);
        }

        internal bool TrySetCache(Cache newCache, int millisecondsTimeout)
        {

            if (!_lock.TryEnterWriteLock(millisecondsTimeout))
            {
                return false;
            }

            Cache = newCache;

            if (_lock.IsWriteLockHeld)
            {
                _lock.ExitWriteLock();
            }

            return true;
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null, int timeout = NO_TIMEOUT)
        {
            if (_disposed)
            {
                return false;
            }
           
            if (!_lock.TryEnterWriteLock(timeout))
            {
                return false;
            }

            Cache.Set(key, value, duration);
            _lock.ExitWriteLock();

            return true;
        }
        public (bool result, DataType? data) TryGet<DataType>(string key, int timeout = NO_TIMEOUT)
        {
            if (_disposed)
            {
                return (false, default);
            }

            if (!_lock.TryEnterReadLock(timeout))
            {
                return (false, default);
            }

            var result = Cache.TryGet<DataType>(key);

            _lock.ExitReadLock();

            return result;
        }

        public bool TryRemove(List<string> keys)
        {
            return TryRemove(keys, timeout: NO_TIMEOUT);
        }

        public bool TryRemove(List<string> keys, int timeout = NO_TIMEOUT)
        {
            if (_disposed)
            {
                return false;
            }

            if (!_lock.TryEnterWriteLock(timeout))
            {
                return false;
            }

            var result = Cache.TryRemove(keys);

            _lock.ExitWriteLock();

            return result;
        }

        public void SaveToFile(Uri path)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            using var _ = ThreadLock();

            Serialize.SaveToFile(this, path);
        }

        public static ThreadCache? TryCreateFromFile(Uri path)
        {
            return Serialize.TryCreateFromFile<ThreadCache>(path);
        }

        public bool TryLoadFromFile(Uri path)
        {
            if (_disposed)
            {
                return false;
            }
            
            using var _ = ThreadLock();

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
                Cache = loadedData.Cache;

                return true;
            }
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ThreadCacheManager.Remove(Key);

                    Cache.Dispose();
                    _lock.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion

        #region IEquatable
        public bool Equals(ThreadCache? other)
        {
            return other != null &&
                   Cache.Equals(other.Cache) &&
                   Key.Equals(other.Key);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ThreadCache);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Cache, Key);
        }
        #endregion
    }
}
