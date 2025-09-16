using Celestus.Serialization;
using System.Text.Json.Serialization;

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

        public CacheLock ThreadLock(TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return new CacheLock(_lock, timeout ?? TimeSpan.FromMilliseconds(DEFAULT_TIMEOUT_IN_MS));
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

            if (!_lock.TryEnterReadLock(timeout ?? TimeSpan.FromMilliseconds(DEFAULT_TIMEOUT_IN_MS)))
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

        public (bool result, DataType data) TryGet<DataType>(string key, int timeout)
        {
            return TryGet<DataType>(key, TimeSpan.FromMilliseconds(timeout));
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null, TimeSpan? timeout = null)
        {
            return TrySet(key, value, timeout ?? TimeSpan.FromMilliseconds(NO_TIMEOUT), duration);
        }

        public bool TrySet<DataType>(string key, DataType value, int timeoutInMs, TimeSpan? duration = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterWriteLock(timeoutInMs))
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
            return TryRemove(keys, timeout ?? TimeSpan.FromMilliseconds(DEFAULT_TIMEOUT_IN_MS));
        }

        public bool TryRemove(string[] keys, int timeout)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterWriteLock(timeout))
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
            return TryGet<DataType>(key, DEFAULT_TIMEOUT_IN_MS);
        }

        public override bool TryRemove(string[] keys)
        {
            return TryRemove(keys, timeout: NO_TIMEOUT);
        }

        public override bool TrySaveToFile(Uri path)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (!_lock.TryEnterReadLock(DEFAULT_TIMEOUT_IN_MS))
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

            if (!_lock.TryEnterWriteLock(DEFAULT_TIMEOUT_IN_MS))
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

        public override bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            throw new NotImplementedException();
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
