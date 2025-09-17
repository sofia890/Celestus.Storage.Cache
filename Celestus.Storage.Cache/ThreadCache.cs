using Celestus.Serialization;
using System.Text.Json.Serialization;
using System.Threading;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(ThreadCacheJsonConverter))]
    public partial class ThreadCache : CacheBase<string>, IDisposable
    {
        const int CLEANER_INTERVAL_IN_MS = 5000;
        public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromMilliseconds(5000);

        private bool _disposed = false;

        internal Cache Cache { get; set; }

        readonly ReaderWriterLockSlim _lock = new();


        public ThreadCache(string key, Cache cache, bool persistent = false, string persistentStorageLocation = "") :
            base(key, persistent: persistent, persistentStorageLocation)
        {
            // Not persistent or no persistent data loaded.
            if (!persistent || Cache == null)
            {
                Cache = cache;
            }

            Cache.Cleaner.RegisterCache(new(this));
        }

        public ThreadCache(string key, CacheCleanerBase<string> cleaner, bool persistent = false, string persistentStorageLocation = "") :
            this(key, new Cache(cleaner), persistent: persistent, persistentStorageLocation: persistentStorageLocation)
        {
        }

        public ThreadCache(CacheCleanerBase<string> cleaner) :
            this(string.Empty, cleaner)
        {
        }

        public ThreadCache(string key) : this(key, new Cache())
        {
        }

        public ThreadCache(string key, TimeSpan? cleaningInterval, bool persistent = false, string persistentStorageLocation = "") :
            this(key,
                cleaner: new ThreadCacheCleaner<string>(cleaningInterval ?? TimeSpan.FromMilliseconds(CLEANER_INTERVAL_IN_MS)),
                persistent: persistent,
                persistentStorageLocation: persistentStorageLocation)
        {
        }

        public ThreadCache(TimeSpan? cleaningInterval = null, bool persistent = false, string persistentStorageLocation = "") :
            this(string.Empty, cleaningInterval, persistent: persistent, persistentStorageLocation: persistentStorageLocation)
        {
        }

        public ThreadCache(string key, bool persistent, string persistentStorageLocation = "") :
            this(key, TimeSpan.FromMilliseconds(CLEANER_INTERVAL_IN_MS), persistent: persistent, persistentStorageLocation: persistentStorageLocation)
        {
        }

        private TimeSpan ValueOrDefault(TimeSpan? timeout = null)
        {
            return timeout ?? DefaultTimeout;
        }

        public CacheLock ThreadLock(TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return new CacheLock(_lock, ValueOrDefault(timeout));
        }

        internal bool TrySetCache(Cache newCache, TimeSpan timeout)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterWriteLock(timeout))
            {
                return false;
            }

            try
            {
                Cache = newCache;

                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public (bool result, DataType data) TryGet<DataType>(string key, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterReadLock(ValueOrDefault(timeout)))
            {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
                return (false, default);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
            }

            try
            {
                return Cache.TryGet<DataType>(key);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterWriteLock(ValueOrDefault(timeout)))
            {
                return false;
            }

            try
            {
                Cache.Set(key, value, out var entry, duration);

                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryRemove(string[] keys, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterWriteLock(ValueOrDefault(timeout)))
            {
                return false;
            }

            try
            {
                return Cache.TryRemove(keys);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #region CacheBase<string>

        internal override CacheCleanerBase<string> Cleaner { get => Cache.Cleaner; }

        internal override Dictionary<string, CacheEntry> Storage { get => Cache.Storage; set => Cache.Storage = value; }


        public override void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _lock.EnterWriteLock();

            try
            {
                Cache.Set(key, value, duration);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public override DataType Get<DataType>(string key)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _lock.EnterReadLock();
            try
            {
                return Cache.Get<DataType>(key);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public override (bool result, DataType data) TryGet<DataType>(string key)
        {
            return TryGet<DataType>(key, DefaultTimeout);
        }

        public override bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            return TrySet(key, value, duration: duration, timeout: null); 
        }

        public override bool TryRemove(string[] keys)
        {
            return TryRemove(keys, timeout: null);
        }

        public override bool TrySaveToFile(Uri path)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterReadLock(DefaultTimeout))
            {
                return false;
            }

            try
            {
                Serialize.SaveToFile(this, path);

                return true;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public static ThreadCache? TryCreateFromFile(Uri path)
        {
            return Serialize.TryCreateFromFile<ThreadCache>(path);
        }

        public override bool TryLoadFromFile(Uri path)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterWriteLock(DefaultTimeout))
            {
                return false;
            }

            try
            {
                bool result = false;

                var loadedData = Serialize.TryCreateFromFile<ThreadCache>(path);

                if (loadedData != null && Key == loadedData.Key)
                {
                    Cache = loadedData.Cache;

                    result = true;
                }

                return result;
            }
            catch
            {
                throw;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        #endregion

        #region IDisposable
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ThreadCache()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                HandlePersistentFinalization();

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

        #region ICloneable
        public override object Clone()
        {
            return new ThreadCache(Key, (Cache)Cache.Clone(), Persistent, PersistentStorageLocation?.AbsolutePath ?? string.Empty);
        }
        #endregion
    }
}
