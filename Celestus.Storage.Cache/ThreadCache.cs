using Celestus.Exceptions;
using Celestus.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(ThreadCacheJsonConverter))]
    public partial class ThreadCache : CacheBase<string>, IDisposable
    {
        const int CLEANER_INTERVAL_IN_MS = 5000;
        public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromMilliseconds(5000);

        private bool _disposed = false;

        private Cache? _cache;
        internal Cache Cache
        {
            get => _cache!;
            private set
            {
                if (_cache != value)
                {
                _cache = value;
                _cache.Cleaner.RegisterCache(new(this));
            }
        }
        }

        readonly ReaderWriterLockSlim _lock = new();

        public ThreadCache(string key,
                           Cache cache,
                           bool persistenceEnabled = false,
                           string persistenceStorageLocation = "") : base(key)
        {
            // Not persistenceEnabled or no persistenceEnabled data loaded.
            // Base class constructor calls TryLoadFromFile(...) when persistence is enabled.
            // Need to check if Cache is already set.
            if (!persistenceEnabled || Cache == null)
            {
                Cache = cache;
            }
        }

        public ThreadCache(string key, CacheCleanerBase<string> cleaner, bool persistenceEnabled = false, string persistenceStorageLocation = "") :
            this(key, new Cache(cleaner,
                                persistenceEnabled: persistenceEnabled,
                                persistenceStorageLocation: persistenceStorageLocation))
        {
        }

        public ThreadCache(CacheCleanerBase<string> cleaner) :
            this(string.Empty, cleaner)
        {
        }

        public ThreadCache(string key) : this(key, new Cache())
        {
        }

        public ThreadCache(string key, TimeSpan? cleaningInterval, bool persistenceEnabled = false, string persistenceStorageLocation = "") :
            this(key,
                cleaner: new ThreadCacheCleaner<string>(cleaningInterval ?? TimeSpan.FromMilliseconds(CLEANER_INTERVAL_IN_MS)),
                persistenceEnabled: persistenceEnabled,
                persistenceStorageLocation: persistenceStorageLocation)
        {
        }

        public ThreadCache(TimeSpan? cleaningInterval = null, bool persistenceEnabled = false, string persistenceStorageLocation = "") :
            this(string.Empty, cleaningInterval, persistenceEnabled: persistenceEnabled, persistenceStorageLocation: persistenceStorageLocation)
        {
        }

        public ThreadCache(string key, bool persistenceEnabled, string persistenceStorageLocation = "") :
            this(key, TimeSpan.FromMilliseconds(CLEANER_INTERVAL_IN_MS), persistenceEnabled: persistenceEnabled, persistenceStorageLocation: persistenceStorageLocation)
        {
        }

        private TimeSpan GetTimeout(TimeSpan? timeout = null)
        {
            return timeout ?? DefaultTimeout;
        }

        public bool TryGetThreadWriteLock([MaybeNullWhen(false)] out CacheLock cacheLock, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return CacheLock.TryWriteLock(_lock, GetTimeout(timeout), out cacheLock);
        }

        public bool TryGetThreadReadLock([MaybeNullWhen(false)] out CacheLock cacheLock, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return CacheLock.TryReadLock(_lock, GetTimeout(timeout), out cacheLock);
        }

        internal bool TrySetCache(Cache newCache, TimeSpan timeout)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return DoWhileWriteLocked(
                () =>
                {
                    Cache.Dispose();

                    Cache = newCache;
                },
                timeout);
        }

        public (bool result, DataType data) TryGet<DataType>(string key, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (DoWhileReadLocked(() => Cache.TryGet<DataType>(key), out var result, timeout))
            {
                return result;
            }
            else
            {
                return (false, default!);
            }
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return DoWhileWriteLocked(() => Cache.TrySet(key, value, duration),
                                      out var result,
                                      GetTimeout(timeout)) && result;
        }

        public bool TryRemove(string[] keys, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return DoWhileWriteLocked(() => Cache.TryRemove(keys), out var result, GetTimeout(timeout)) && result;
        }

        public bool TryRemove(string key, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return DoWhileWriteLocked(() => Cache.TryRemove(key), out var result, GetTimeout(timeout)) && result;
        }

        private bool DoWhileReadLocked<ReturnType>(Func<ReturnType> act, out ReturnType result, TimeSpan? timeout)
        {
            if (TryGetThreadReadLock(out var cacheLock, GetTimeout(timeout)))
            {
                using (cacheLock)
                {
                    result = act();

                    return true;
                }
            }
            else
            {
                result = default!;

                return false;
            }
        }

        private bool DoWhileReadLocked(Action act, TimeSpan? timeout)
        {
            if (TryGetThreadReadLock(out var cacheLock, GetTimeout(timeout)))
            {
                using (cacheLock)
                {
                    act();

                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        private bool DoWhileWriteLocked<ReturnType>(Func<ReturnType> act, out ReturnType result, TimeSpan? timeout)
        {
            if (TryGetThreadWriteLock(out var cacheLock, GetTimeout(timeout)))
            {
                using (cacheLock)
                {
                    result = act();

                    return true;
                }
            }
            else
            {
                result = default!;

                return false;
            }
        }

        private bool DoWhileWriteLocked(Action act, TimeSpan? timeout)
        {
            if (TryGetThreadWriteLock(out var cacheLock, GetTimeout(timeout)))
            {
                using (cacheLock)
                {
                    act();

                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        #region CacheBase<string>

        [MemberNotNullWhen(true, nameof(PersistenceStoragePath))]
        public override bool PersistenceEnabled { get => Cache?.PersistenceEnabled ?? false; }
        public override Uri? PersistenceStoragePath
        {
            get => Cache?.PersistenceStoragePath ?? null;
            set => Cache.PersistenceStoragePath = value;
        }

        internal override CacheCleanerBase<string> Cleaner
        {
            get => Cache.Cleaner;
            set => Cache.Cleaner = value;
        }

        internal override Dictionary<string, CacheEntry> Storage { get => Cache.Storage; set => Cache.Storage = value; }


        public override void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            DoWhileWriteLocked(() => Cache.Set(key, value, duration), DefaultTimeout);
        }

        public override DataType Get<DataType>(string key)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _ = DoWhileReadLocked(() => Cache.Get<DataType>(key), out var result, DefaultTimeout);

            return result;
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

        public override bool TryRemove(string key)
        {
            return TryRemove(key, timeout: null);
        }

        public override bool TrySaveToFile(Uri path)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return DoWhileReadLocked(() => Serialize.SaveToFile(this, path), DefaultTimeout);
        }

        public static ThreadCache? TryCreateFromFile(Uri path)
        {
            return Serialize.TryCreateFromFile<ThreadCache>(path);
        }

        public override bool TryLoadFromFile(Uri path)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            bool TryLoad()
            {
                bool result = false;

                var loadedData = Serialize.TryCreateFromFile<ThreadCache>(path);

                if (loadedData != null && Key == loadedData.Key &&
                    PersistenceEnabled == loadedData.PersistenceEnabled &&
                    PersistenceStoragePath == loadedData.PersistenceStoragePath)
                {
                    Cache.Dispose();

                    Cache = loadedData.Cache;

                    result = true;
                }

                return result;
            }

            return DoWhileWriteLocked(TryLoad, out var result, DefaultTimeout) && result;
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
            try
            {
                Dispose(false);
            }
            catch
            {
                // Suppress all exceptions
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _ = DoWhileWriteLocked(
                    () =>
                    {
                        Cache.HandlePersistenceEnabledFinalization();

                        if (disposing)
                        {
                            Factory.Remove(Key);

                            Cache.Dispose();
                        }

                        _disposed = true;
                    },
                    DefaultTimeout);

                if (disposing)
                {
                    _lock.Dispose();
                }
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
            return HashCode.Combine(Cache.GetHashCode(), Key);
        }
        #endregion

        #region ICloneable
        public override object Clone()
        {
            return new ThreadCache(Key, (Cache)Cache.Clone(), PersistenceEnabled, PersistenceStoragePath?.AbsolutePath ?? string.Empty);
        }
        #endregion
    }
}
