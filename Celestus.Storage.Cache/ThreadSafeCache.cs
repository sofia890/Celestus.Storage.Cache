using Celestus.Exceptions;
using Celestus.Serialization;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    class LockException(string message) : Exception(message);

    class ReadLockException(string message) : LockException(message)
    {
        public static void ThrowIf(bool condition, string message)
        {
            Condition.ThrowIf<ReadLockException>(condition, message);
        }
    }

    class WriteLockException(string message) : LockException(message)
    {
        public static void ThrowIf(bool condition, string message)
        {
            Condition.ThrowIf<WriteLockException>(condition, message);
        }
    }

    /// <summary>
    /// Thread safe cache implementation with optional persistence to file.
    /// </summary>
    [JsonConverter(typeof(ThreadSafeCacheJsonConverter))]
    public partial class ThreadSafeCache : CacheBase<string, string>, IDisposable
    {
        public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromMilliseconds(5000);
        public static TimeSpan DefaultCleanerInterval { get; } = TimeSpan.FromMilliseconds(60000);

        private CacheBase<string, string>? _cache;
        internal CacheBase<string, string> Cache
        {
            get => _cache!;
            private set
            {
                if (_cache != value)
                {
                    _cache?.Cleaner.UnregisterCache();

                    _cache = value;
                    _cache.Cleaner.RegisterCache(new(this));
                }
            }
        }

        readonly ReaderWriterLockSlim _lock = new();

        public ThreadSafeCache(CacheBase<string, string> cache,
                               bool persistenceEnabled = false,
                               string persistenceStorageLocation = "")
        {
            // Not persistenceEnabled or no persistenceEnabled data loaded.
            // Base class constructor calls TryLoadFromFile(...) when persistence is enabled.
            // Need to check if Cache is already set.
            if (!persistenceEnabled || Cache == null)
            {
                Cache = cache;
            }
        }

        public ThreadSafeCache(string id, CacheCleanerBase<string, string> cleaner, bool persistenceEnabled = false, string persistenceStorageLocation = "") :
            this(new Cache(id,
                           [],
                           cleaner,
                           persistenceEnabled: persistenceEnabled,
                           persistenceStorageLocation: persistenceStorageLocation))
        {
        }

        public ThreadSafeCache(CacheCleanerBase<string, string> cleaner) :
            this(string.Empty, cleaner)
        {
        }

        public ThreadSafeCache(string id) : this(new Cache(id))
        {
        }

        public ThreadSafeCache(string id, TimeSpan? cleaningInterval, bool persistenceEnabled = false, string persistenceStorageLocation = "") :
            this(id,
                 cleaner: new ThreadSafeCacheCleaner<string, string>(cleaningInterval ?? DefaultCleanerInterval),
                 persistenceEnabled: persistenceEnabled,
                 persistenceStorageLocation: persistenceStorageLocation)
        {
        }

        public ThreadSafeCache(TimeSpan? cleaningInterval = null, bool persistenceEnabled = false, string persistenceStorageLocation = "") :
            this(string.Empty, cleaningInterval, persistenceEnabled: persistenceEnabled, persistenceStorageLocation: persistenceStorageLocation)
        {
        }

        public ThreadSafeCache(string id, bool persistenceEnabled, string persistenceStorageLocation = "") :
            this(id, DefaultCleanerInterval, persistenceEnabled: persistenceEnabled, persistenceStorageLocation: persistenceStorageLocation)
        {
        }

        public static ThreadSafeCache? TryCreateFromFile(FileInfo file)
        {
            return Serialize.TryCreateFromFile<ThreadSafeCache>(file);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TimeSpan GetTimeout(TimeSpan? timeout = null)
        {
            return timeout ?? DefaultTimeout;
        }

        public bool TryGetWriteLock([MaybeNullWhen(false)] out CacheLock cacheLock, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return CacheLock.TryWriteLock(_lock, GetTimeout(timeout), out cacheLock);
        }

        public bool TryGetReadLock([MaybeNullWhen(false)] out CacheLock cacheLock, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return CacheLock.TryReadLock(_lock, GetTimeout(timeout), out cacheLock);
        }

        internal bool TrySetCache(CacheBase<string, string> newCache, TimeSpan timeout)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return DoWhileWriteLocked(
                () =>
                {
                    ReplaceCache(newCache);
                },
                timeout);
        }

        public bool TryGet<DataType>(string key, [MaybeNullWhen(false)] out DataType data, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            (bool success, DataType value) TryGetLocal()
            {
                var innerResult = Cache.TryGet<DataType>(key, out var innerData);

                return (innerResult, innerData!); 
            }

            if (DoWhileReadLocked(() => TryGetLocal(), out var result, timeout) && result.success)
            {
                data = result.value;

                return true;
            }
            else
            {
                data = default;

                return false;
            }
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration, TimeSpan? timeout = null)
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
            if (TryGetReadLock(out var cacheLock, GetTimeout(timeout)))
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
            if (TryGetReadLock(out var cacheLock, GetTimeout(timeout)))
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
            if (TryGetWriteLock(out var cacheLock, GetTimeout(timeout)))
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
            if (TryGetWriteLock(out var cacheLock, GetTimeout(timeout)))
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

        private void ReplaceCache(CacheBase<string, string> newCache)
        {
            // Assignment causes cleaner of old cache to unregister ThreadCache. Need to dispose after cleanup.
            var oldCache = Cache;

            Cache = newCache;

            oldCache?.Dispose();
        }

        #region CacheBase<string, string>
        public string Id => Cache.Id;


        [MemberNotNullWhen(true, nameof(PersistenceStorageFile))]
        public bool PersistenceEnabled { get => Cache?.PersistenceEnabled ?? false; }

        public FileInfo? PersistenceStorageFile
        {
            get => Cache?.PersistenceStorageFile ?? null;
            set
            {
                Condition.ThrowIf<InvalidOperationException>(value == null, "Cannot set persistence storage path before cache is set.");

                Cache.PersistenceStorageFile = value;
            }
        }

        public CacheCleanerBase<string, string> Cleaner
        {
            get => Cache.Cleaner;
            set => Cache.Cleaner = value;
        }

        public Dictionary<string, CacheEntry> Storage { get => Cache.Storage; set => Cache.Storage = value; }


        public void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            var result = DoWhileWriteLocked(() => Cache.Set(key, value, duration), DefaultTimeout);

            WriteLockException.ThrowIf(!result, "Could not acquire write lock.");
        }

        public DataType Get<DataType>(string key)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            var result = DoWhileReadLocked(() => Cache.Get<DataType>(key), out var value, DefaultTimeout);

            ReadLockException.ThrowIf(!result, "Could not acquire read lock.");

            return value;
        }

        public bool TryGet<DataType>(string key, [MaybeNullWhen(false)] out DataType data)
        {
            return TryGet(key, out data, DefaultTimeout);
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            return TrySet(key, value, duration: duration, timeout: null);
        }

        public bool TryRemove(string[] keys)
        {
            return TryRemove(keys, timeout: null);
        }

        public bool TryRemove(string key)
        {
            return TryRemove(key, timeout: null);
        }

        public bool TrySaveToFile(FileInfo file)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            try
            {
                return DoWhileReadLocked(() => Serialize.SaveToFile(this, file), DefaultTimeout);
            }
            catch
            {
                return false;
            }
        }

        public bool TryLoadFromFile(FileInfo file)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            bool TryLoad()
            {
                bool result = false;

                var loadedData = Serialize.TryCreateFromFile<ThreadSafeCache>(file);

                if (loadedData != null && Id == loadedData.Id)
                {
                    if (PersistenceEnabled != loadedData.PersistenceEnabled)
                    {
                        return false;
                    }
                    else if (PersistenceEnabled &&
                            PersistenceStorageFile?.FullName != loadedData.PersistenceStorageFile?.FullName)
                    {
                        return false;
                    }

                    ReplaceCache(loadedData.Cache);

                    result = true;
                }

                return result;
            }

            return DoWhileWriteLocked(TryLoad, out var result, DefaultTimeout) && result;
        }

        public ImmutableDictionary<string, CacheEntry> GetEntries()
        {
            var result = DoWhileReadLocked(() => Cache.GetEntries(), out var dictionary, DefaultTimeout);

            ReadLockException.ThrowIf(!result, "Could not acquire read lock.");

            return dictionary;
        }
        #endregion

        #region IDisposable
        private bool _disposed = false;


        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _ = DoWhileWriteLocked(
                    () =>
                    {
                        Factory.Remove(Id);

                        if (disposing)
                        {
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
        #endregion

        #region IEquatable
        public bool Equals(ThreadSafeCache? other)
        {
            return other != null &&
                   Cache.Equals(other.Cache) &&
                   Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ThreadSafeCache);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Cache.GetHashCode(), Id);
        }
        #endregion

        #region ICloneable
        /// <returns>Shallow clone of the cache.</returns>
        public object Clone()
        {
            return new ThreadSafeCache((Cache)Cache.Clone(), PersistenceEnabled, PersistenceStorageFile?.FullName ?? string.Empty);
        }
        #endregion
    }
}
