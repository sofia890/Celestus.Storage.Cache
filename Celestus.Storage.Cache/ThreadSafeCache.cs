using Celestus.Exceptions;
using Celestus.Serialization;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    class LockException(string message) : Exception(message);
    class ReadLockException(string message) : LockException(message)
    {
        public static void ThrowIf(bool condition, string message) => Condition.ThrowIf<ReadLockException>(condition, message);
    }
    class WriteLockException(string message) : LockException(message)
    {
        public static void ThrowIf(bool condition, string message) => Condition.ThrowIf<WriteLockException>(condition, message);
    }

    [JsonConverter(typeof(ThreadSafeCacheJsonConverter))]
    public partial class ThreadSafeCache : ICacheBase<string, string>, IDisposable
    {
        public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromMilliseconds(5000);
        public static TimeSpan DefaultCleanerInterval { get; } = TimeSpan.FromMilliseconds(60000);

        private ICacheBase<string, string>? _cache;
        internal ICacheBase<string, string> Cache
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

        private readonly ReaderWriterLockSlim _lock = new();

        public ThreadSafeCache(string id,
                               bool persistenceEnabled = false,
                               string persistenceStorageLocation = "",
                               BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw)
            : this(new Cache(id,
                             [],
                             new CacheCleaner<string, string>(),
                             blockedEntryBehavior: blockedEntryBehavior,
                             persistenceEnabled: persistenceEnabled,
                             persistenceStorageLocation: persistenceStorageLocation),
                   persistenceEnabled: persistenceEnabled,
                   persistenceStorageLocation: persistenceStorageLocation,
                   blockedEntryBehavior: blockedEntryBehavior)
        { }

        public ThreadSafeCache(string id, bool persistenceEnabled, string persistenceStorageLocation)
            : this(id, persistenceEnabled: persistenceEnabled, persistenceStorageLocation: persistenceStorageLocation, blockedEntryBehavior: BlockedEntryBehavior.Throw) { }

        public ThreadSafeCache(string id,
                               CacheCleanerBase<string, string> cleaner,
                               BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw)
            : this(new Cache(id,
                             [],
                             cleaner,
                             blockedEntryBehavior: blockedEntryBehavior,
                             persistenceEnabled: false,
                             persistenceStorageLocation: ""),
                   persistenceEnabled: false,
                   persistenceStorageLocation: "",
                   blockedEntryBehavior: blockedEntryBehavior)
        { }

        public ThreadSafeCache(CacheCleanerBase<string, string> cleaner, BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw)
            : this(string.Empty, cleaner, blockedEntryBehavior) { }

        public ThreadSafeCache(TimeSpan? cleaningInterval)
            : this(string.Empty,
                   cleaner: new ThreadSafeCacheCleaner<string, string>(cleaningInterval ?? DefaultCleanerInterval),
                   blockedEntryBehavior: BlockedEntryBehavior.Throw)
        { }

        public ThreadSafeCache(ICacheBase<string, string> cache,
                               bool persistenceEnabled = false,
                               string persistenceStorageLocation = "",
                               BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw)
        {
            if (!persistenceEnabled || Cache == null)
            {
                Cache = cache;
            }

            Cache.BlockedEntryBehavior = blockedEntryBehavior;
        }

        public ThreadSafeCache(string id,
                               CacheCleanerBase<string, string> cleaner,
                               bool persistenceEnabled,
                               string persistenceStorageLocation,
                               BlockedEntryBehavior blockedEntryBehavior)
            : this(new Cache(id,
                             [],
                             cleaner,
                             blockedEntryBehavior: blockedEntryBehavior,
                             persistenceEnabled: persistenceEnabled,
                             persistenceStorageLocation: persistenceStorageLocation),
                   persistenceEnabled: persistenceEnabled,
                   persistenceStorageLocation: persistenceStorageLocation,
                   blockedEntryBehavior: blockedEntryBehavior)
        { }

        public ThreadSafeCache(string id,
                               TimeSpan? cleaningInterval,
                               bool persistenceEnabled,
                               string persistenceStorageLocation,
                               BlockedEntryBehavior blockedEntryBehavior)
            : this(id,
                   cleaner: new ThreadSafeCacheCleaner<string, string>(cleaningInterval ?? DefaultCleanerInterval),
                   persistenceEnabled: persistenceEnabled,
                   persistenceStorageLocation: persistenceStorageLocation,
                   blockedEntryBehavior: blockedEntryBehavior)
        { }

        public ThreadSafeCache(string id,
                               TimeSpan? cleaningInterval,
                               BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw)
            : this(id,
                   cleaningInterval,
                   persistenceEnabled: false,
                   persistenceStorageLocation: string.Empty,
                   blockedEntryBehavior: blockedEntryBehavior)
        { }

        public ThreadSafeCache() : this(string.Empty) { }

        /// <summary>
        /// Tries to create a thread-safe cache from a persistence file. Applies a fresh type register context.
        /// </summary>
        public static ThreadSafeCache? TryCreateFromFile(FileInfo file, BlockedEntryBehavior behaviourMode = BlockedEntryBehavior.Throw, CacheTypeFilterMode filterMode = CacheTypeFilterMode.Blacklist, IEnumerable<Type>? types = null)
        {
            var options = new JsonSerializerOptions();
            options.SetBlockedEntryBehavior(behaviourMode);
            options.SetCacheTypeRegister(new(filterMode, types ?? []));

            return Serialize.TryCreateFromFile<ThreadSafeCache>(file, options);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TimeSpan GetTimeout(TimeSpan? timeout = null) => timeout ?? DefaultTimeout;

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

        internal bool TrySetCache(ICacheBase<string, string> newCache, TimeSpan timeout)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return DoWhileWriteLocked(() => ReplaceCache(newCache), timeout);
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

        private void ReplaceCache(ICacheBase<string, string> newCache)
        {
            var oldCache = Cache;

            Cache = newCache;

            oldCache?.Dispose();
        }

        #region CacheBase<string, string>
        public string Id => Cache.Id;
        [MemberNotNullWhen(true, nameof(PersistenceStorageFile))]
        public bool PersistenceEnabled => Cache?.PersistenceEnabled ?? false;
        public FileInfo? PersistenceStorageFile
        {
            get => Cache?.PersistenceStorageFile ?? null;
            set
            {
                Condition.ThrowIf<InvalidOperationException>(Cache == null, "Cannot set persistence storage path before cache is set.");
                
                Cache.PersistenceStorageFile = value;
            }
        }

        public CacheCleanerBase<string, string> Cleaner
        {
            get => Cache.Cleaner;
            set => Cache.Cleaner = value;
        }

        public CacheTypeRegister TypeRegister
        {
            get => Cache.TypeRegister;
            set => Cache.TypeRegister = value;
        }

        public BlockedEntryBehavior BlockedEntryBehavior
        {
            get => Cache.BlockedEntryBehavior;

            set
            {
                Cache.BlockedEntryBehavior = value;
            }
        }

        public ImmutableDictionary<string, CacheEntry> Storage
        {
            get
            {
                var result = DoWhileReadLocked(() => Cache.Storage, out var dict, DefaultTimeout);

                WriteLockException.ThrowIf(!result, "Could not acquire read lock.");

                return dict;
            }
        }
        
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

        public bool TryGet<DataType>(string key, [MaybeNullWhen(false)] out DataType data) => TryGet(key, out data, DefaultTimeout);

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null) => TrySet(key, value, duration: duration, timeout: null);

        public bool TryRemove(string[] keys) => TryRemove(keys, timeout: null);

        public bool TryRemove(string key) => TryRemove(key, timeout: null);

        public bool TrySaveToFile(FileInfo file)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            bool save()
            {
                var options = new JsonSerializerOptions();
                options.SetBlockedEntryBehavior(BlockedEntryBehavior);
                options.SetCacheTypeRegister(TypeRegister);

                try
                {
                    Serialize.SaveToFile(this, file, options);

                    return true;
                }
                catch
                {
                    return false;
                }
            }

            try { return DoWhileReadLocked(save, out var result, DefaultTimeout) && result; } catch { return false; }
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
        private bool _disposed;

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
                if (Id != null)
                {
                    Factory.Remove(Id);
                }

                var result = DoWhileWriteLocked(
                    () =>
                    {
                        if (disposing)
                        {
                            Cache.Dispose();
                        }

                        _disposed = true;
                    },
                    DefaultTimeout);

                WriteLockException.ThrowIf(!result, "Could not acquire write lock while tearing object down.");

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

        public override bool Equals(object? obj) => Equals(obj as ThreadSafeCache);

        public override int GetHashCode() => HashCode.Combine(Cache.GetHashCode(), Id);
        #endregion

        #region ICloneable

        public object Clone()
        {
            return new ThreadSafeCache((ICacheBase<string, string>)Cache.Clone(),
                                       PersistenceEnabled,
                                       PersistenceStorageFile?.FullName ?? string.Empty);
        }
        #endregion
    }
}
