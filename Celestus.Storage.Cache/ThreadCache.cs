using Celestus.Serialization;
using System.Text.Json.Serialization;
using System.Threading;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(ThreadCacheJsonConverter))]
    public partial class ThreadCache : CacheBase<string>, IDisposable
    {
        const int CLEANER_INTERVAL_IN_MS = 5000;
        public const int DEFAULT_TIMEOUT_IN_MS = 5000;

        private bool _disposed = false;

        internal Cache Cache { get; set; }

        readonly ReaderWriterLockSlim _lock = new();

        internal override CacheCleanerBase<string> Cleaner { get => Cache.Cleaner; }

        public ThreadCache(string key, Cache cache) : base(key)
        {
            Cache = cache;

            Cache.Cleaner.RegisterRemovalCallback(new(TryRemove));
            Cache.Cleaner.RegisterCollection(new(Cache.Storage));
        }

        public ThreadCache(string key, CacheCleanerBase<string> cleaner) :
            this(key, new Cache(cleaner, doNotSetRemoval: true))
        {
        }

        public ThreadCache(CacheCleanerBase<string> cleaner) :
            this(string.Empty, cleaner)
        {
        }

        public ThreadCache(string key, TimeSpan? cleaningInterval = null) :
            this(key, cleaner: new ThreadCacheCleaner<string>(cleaningInterval ?? TimeSpan.FromMilliseconds(CLEANER_INTERVAL_IN_MS)))
        {
        }

        public ThreadCache(TimeSpan? cleaningInterval = null) :
            this(string.Empty, cleaningInterval)
        {
        }

        public ThreadCache(string key) :
            this(key, TimeSpan.FromMilliseconds(CLEANER_INTERVAL_IN_MS))
        {
        }

        public CacheLock ThreadLock(int timeout = NO_TIMEOUT)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return new CacheLock(_lock, timeout);
        }

        internal bool TrySetCache(Cache newCache, int millisecondsTimeout)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

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

        public override void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _lock.EnterWriteLock();
            Cache.Set(key, value, out var entry, duration);
            _lock.ExitWriteLock();
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null, TimeSpan? timeout = null)
        {
            return TrySet(key, value, timeout?.Milliseconds ?? NO_TIMEOUT, duration);
        }

        public bool TrySet<DataType>(string key, DataType value, int timeoutInMs, TimeSpan? duration = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterWriteLock(timeoutInMs))
            {
                return false;
            }

            Cache.Set(key, value, out var entry, duration);
            _lock.ExitWriteLock();

            return true;
        }

        public override DataType? Get<DataType>(string key)
            where DataType : class
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _lock.EnterReadLock();
            var result = Cache.Get<DataType>(key);
            _lock.ExitReadLock();

            return result;
        }

        public (bool result, DataType? data) TryGet<DataType>(string key, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterReadLock(timeout?.Milliseconds ?? DEFAULT_TIMEOUT_IN_MS))
            {
                return (false, default);
            }

            var result = Cache.TryGet<DataType>(key);

            _lock.ExitReadLock();

            return result;
        }

        public (bool result, DataType? data) TryGet<DataType>(string key, int timeout)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

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

        public bool TryRemove(List<string> keys, TimeSpan? timeout = null)
        {
            return TryRemove(keys, timeout?.Milliseconds ?? DEFAULT_TIMEOUT_IN_MS);
        }

        public bool TryRemove(List<string> keys, int timeout)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterWriteLock(timeout))
            {
                return false;
            }

            var result = Cache.TryRemove(keys);

            _lock.ExitWriteLock();

            return result;
        }

        public bool TrySaveToFile(Uri path)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterReadLock(DEFAULT_TIMEOUT_IN_MS))
            {
                return false;
            }

            Serialize.SaveToFile(this, path);

            _lock.ExitReadLock();

            return true;
        }

        public static ThreadCache? TryCreateFromFile(Uri path)
        {
            return Serialize.TryCreateFromFile<ThreadCache>(path);
        }

        public bool TryLoadFromFile(Uri path)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterWriteLock(DEFAULT_TIMEOUT_IN_MS))
            {
                return false;
            }

            bool result = false;

            try
            {
                var loadedData = Serialize.TryCreateFromFile<ThreadCache>(path);

                if (loadedData != null && Key == loadedData.Key)
                {
                    Cache = loadedData.Cache;

                    result = true;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return result;
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
                    Factory.Remove(Key);

                    Cache.Dispose();

                    _lock.Dispose();
                }

                _disposed = true;
            }
        }

        public override bool IsDisposed => _disposed;
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
