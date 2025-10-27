using Celestus.Exceptions;
using Celestus.Serialization;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    class CacheException(string message) : Exception(message);

    class LockException(string message) : CacheException(message);

    class ReadLockException(string message) : LockException(message)
    {
        public static void ThrowIf(bool condition, string message) => Condition.ThrowIf<ReadLockException>(condition, message);
    }

    class WriteLockException(string message) : LockException(message)
    {
        public static void ThrowIf(bool condition, string message) => Condition.ThrowIf<WriteLockException>(condition, message);
    }

    class CacheNotSetException(string message) : CacheException(message)
    {
        public static void ThrowIf([DoesNotReturnIf(true)] bool condition, string message) => Condition.ThrowIf<CacheNotSetException>(condition, message);
    }

    class CacheNullException(string message) : CacheException(message)
    {
        public static void ThrowIf([DoesNotReturnIf(true)] bool condition, string message) => Condition.ThrowIf<CacheNullException>(condition, message);
    }

    [JsonConverter(typeof(ThreadSafeCacheJsonConverter))]
    public partial class ThreadSafeCache : ICacheBase<string, string>, IDisposable
    {
        public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromMilliseconds(5000);

        public static TimeSpan DefaultCleanerInterval { get; } = TimeSpan.FromMilliseconds(60000);

        private ICacheBase<string, string>? _cache;
        internal ICacheBase<string, string> Cache
        {
            get
            {
                var success = DoWhileReadLocked(() => _cache, out var cache, DefaultTimeout);

                ReadLockException.ThrowIf(!success, "Could not acquire read lock.");
                CacheNullException.ThrowIf(cache == null, "Cache is null.");

                return cache;
            }
            
            private set
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);

                void set()
                {
                    if (_cache != value)
                    {
                        _cache?.Cleaner.UnregisterCache();

                        _cache = value;
                        _cache.Cleaner.RegisterCache(new(this));
                    }
                }

                var lockAquired = DoWhileWriteLocked(() => set(), DefaultTimeout);

                WriteLockException.ThrowIf(!lockAquired, "Could not acquire write lock while setting underlying cache reference.");
            }
        }

        private readonly ReaderWriterLockSlim _lock = new();

        public ThreadSafeCache(string id,
                               bool persistenceEnabled = false,
                               string persistenceStorageLocation = "",
                               BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw,
                               CacheTypeRegister? typeRegister = null)
        : this(new Cache(id,
                         [],
                         new CacheCleaner<string, string>(),
                         blockedEntryBehavior: blockedEntryBehavior,
                         persistenceEnabled: persistenceEnabled,
                         persistenceStorageLocation: persistenceStorageLocation),
                         persistenceEnabled: persistenceEnabled,
                         persistenceStorageLocation: persistenceStorageLocation,
                         blockedEntryBehavior: blockedEntryBehavior,
                         typeRegister: typeRegister)
        {
        }

        public ThreadSafeCache(string id, bool persistenceEnabled, string persistenceStorageLocation)
        : this(id,
              persistenceEnabled: persistenceEnabled,
              persistenceStorageLocation: persistenceStorageLocation,
              blockedEntryBehavior: BlockedEntryBehavior.Throw) { }

        public ThreadSafeCache(string id,
                               CacheCleanerBase<string, string> cleaner,
                               BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw,
                               CacheTypeRegister? typeRegister = null)
            : this(new Cache(id,
                             [],
                             cleaner,
                             blockedEntryBehavior: blockedEntryBehavior,
                             persistenceEnabled: false,
                             persistenceStorageLocation: ""),
                             persistenceEnabled: false,
                             persistenceStorageLocation: "",
                             blockedEntryBehavior: blockedEntryBehavior,
                             typeRegister: typeRegister)
        { 
        }

        public ThreadSafeCache(CacheCleanerBase<string, string> cleaner,
                               BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw,
                               CacheTypeRegister? typeRegister = null)
        : this(string.Empty, cleaner, blockedEntryBehavior, typeRegister) { }

        public ThreadSafeCache(TimeSpan? cleaningInterval)
            : this(string.Empty,
                   cleaner: new ThreadSafeCacheCleaner<string, string>(cleaningInterval ?? DefaultCleanerInterval),
                   blockedEntryBehavior: BlockedEntryBehavior.Throw,
                   typeRegister: null)
        {
        }

        public ThreadSafeCache(ICacheBase<string, string> cache,
                               bool persistenceEnabled = false,
                               string persistenceStorageLocation = "",
                               BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw,
                               CacheTypeRegister? typeRegister = null)
        {
            if (!persistenceEnabled || _cache == null)
            {
                _cache = cache;
            }

            _cache.BlockedEntryBehavior = blockedEntryBehavior;

            if (typeRegister != null)
            {
                _cache.TypeRegister = typeRegister;
            }
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
        public static ThreadSafeCache? TryCreateFromFile(
            FileInfo file,
            BlockedEntryBehavior behaviourMode = BlockedEntryBehavior.Throw,
            CacheTypeFilterMode filterMode = CacheTypeFilterMode.Blacklist,
            IEnumerable<Type>? types = null)
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

        public bool TryGet<DataType>(string key, [MaybeNullWhen(false)] out DataType data, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            (bool success, DataType value) TryGetLocal()
            {
                var innerResult = _cache.TryGet<DataType>(key, out var innerData);

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

            return DoWhileWriteLocked(() => _cache.TrySet(key, value, duration),
                                      out var result,
                                      GetTimeout(timeout)) && result;
        }

        public bool TryRemove(string[] keys, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return DoWhileWriteLocked(() => _cache.TryRemove(keys), out var result, GetTimeout(timeout)) && result;
        }

        public bool TryRemove(string key, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return DoWhileWriteLocked(() => _cache.TryRemove(key), out var result, GetTimeout(timeout)) && result;
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

        private bool TrySetCache(ICacheBase<string, string> newCache, TimeSpan? timeout = null)
        {
            void replace()
            {
                var oldCache = _cache;

                _cache = newCache;

                if (oldCache != null)
                {
                    _cache.BlockedEntryBehavior = oldCache.BlockedEntryBehavior;
                    _cache.TypeRegister = oldCache.TypeRegister;
                }

                oldCache?.Dispose();
            }

            return DoWhileWriteLocked(replace, timeout ?? DefaultTimeout);
        }

        #region CacheBase<string, string>
        public string Id
        {
            get
            {
                var success = DoWhileReadLocked(() => _cache!.Id, out var id, DefaultTimeout);

                ReadLockException.ThrowIf(!success, "Could not acquire read lock for Id.");

                return id;
            }
        }

        [MemberNotNullWhen(true, nameof(PersistenceStorageFile))]
        public bool PersistenceEnabled
        {
            get
            {
                bool get()
                {
                    return (_cache?.PersistenceEnabled ?? false) && (_cache?.PersistenceStorageFile != null);
                }

                var success = DoWhileReadLocked(get, out var enabled, DefaultTimeout);
                
                ReadLockException.ThrowIf(!success, "Could not acquire read lock.");
                
                return enabled;
            }
        }

        public FileInfo? PersistenceStorageFile
        {
            get
            {
                FileInfo? get()
                {
                    return _cache?.PersistenceStorageFile;
                }

                var success = DoWhileReadLocked(get, out var file, DefaultTimeout);

                ReadLockException.ThrowIf(!success, "Could not acquire read lock.");

                return file;
            }

            set
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);

                void set()
                {
                    CacheNotSetException.ThrowIf(_cache is null, "Cannot set persistence storage path before cache is set.");
                    _cache.PersistenceStorageFile = value;
                }

                var success = DoWhileWriteLocked(set, DefaultTimeout);

                WriteLockException.ThrowIf(!success, "Could not acquire write lock for PersistenceStorageFile.");
            }
        }

        public CacheCleanerBase<string, string> Cleaner
        {
            get
            {
                var success = DoWhileReadLocked(() => _cache!.Cleaner, out var cleaner, DefaultTimeout);
                ReadLockException.ThrowIf(!success, "Could not acquire read lock for Cleaner.");
                return cleaner;
            }

            set
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                void setAction()
                {
                    CacheNotSetException.ThrowIf(_cache is null, "Cannot set cleaner before cache is set.");
                    _cache.Cleaner = value;
                }
                var success = DoWhileWriteLocked(setAction, DefaultTimeout);
                WriteLockException.ThrowIf(!success, "Could not acquire write lock for Cleaner.");
            }
        }

        public CacheTypeRegister TypeRegister
        {
            get
            {
                var success = DoWhileReadLocked(() => _cache!.TypeRegister, out var typeRegister, DefaultTimeout);

                ReadLockException.ThrowIf(!success, "Could not acquire read lock for TypeRegister.");

                return typeRegister;
            }

            set
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                void setAction()
                {
                    CacheNotSetException.ThrowIf(_cache is null, "Cannot set type register before cache is set.");
                    _cache.TypeRegister = value;
                }
                var success = DoWhileWriteLocked(setAction, DefaultTimeout);
                WriteLockException.ThrowIf(!success, "Could not acquire write lock for TypeRegister.");
            }
        }

        public BlockedEntryBehavior BlockedEntryBehavior
        {
            get
            {
                var success = DoWhileReadLocked(() => _cache!.BlockedEntryBehavior, out var behavior, DefaultTimeout);
                ReadLockException.ThrowIf(!success, "Could not acquire read lock for BlockedEntryBehavior.");
                return behavior;
            }

            set
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);

                void set()
                {
                    CacheNotSetException.ThrowIf(_cache == null, "Cannot set blocked type behaviour before cache is set.");

                    if (_cache.BlockedEntryBehavior != value)
                    {
                        _cache.BlockedEntryBehavior = value;
                    }
                }

                var lockAquired = DoWhileWriteLocked(set, DefaultTimeout);

                WriteLockException.ThrowIf(!lockAquired, "Could not acquire write lock for BlockedEntryBehavior.");
            }
        }

        public ImmutableDictionary<string, CacheEntry> Storage
        {
            get
            {
                var result = DoWhileReadLocked(() => _cache.Storage, out var dict, DefaultTimeout);

                ReadLockException.ThrowIf(!result, "Could not acquire read lock.");

                return dict;
            }
        }

        public void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            var result = DoWhileWriteLocked(() => _cache.Set(key, value, duration), DefaultTimeout);

            WriteLockException.ThrowIf(!result, "Could not acquire write lock.");
        }

        public DataType Get<DataType>(string key)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            var result = DoWhileReadLocked(() => _cache.Get<DataType>(key), out var value, DefaultTimeout);

            ReadLockException.ThrowIf(!result, "Could not acquire read lock.");

            return value;
        }

        public bool TryGet<DataType>(string key, [MaybeNullWhen(false)] out DataType data) => TryGet(key, out data, DefaultTimeout);

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null) => TrySet(key, value, duration: duration, timeout: null);

        public bool TryRemove(string[] keys) => TryRemove(keys, timeout: null);

        public bool TryRemove(string key) => TryRemove(key, timeout: null);

        public bool TrySaveToFile(FileInfo file)
        {
            return TrySaveToFile(file, out _);
        }

        public bool TrySaveToFile(FileInfo file, out Exception? error)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            Exception? capturedError = null;

            bool save()
            {
                var options = new JsonSerializerOptions();
                options.SetBlockedEntryBehavior(BlockedEntryBehavior);
                options.SetCacheTypeRegister(TypeRegister);

                return Serialize.TrySaveToFile(this, file, out capturedError, options);
            }

            try
            {
                var success = DoWhileReadLocked(save, out var result, DefaultTimeout) && result;
                
                if (!success && capturedError != null)
                {
                    error = capturedError;
                }
                else
                {
                    error = null;
                }

                return success;
            }
            catch (Exception exception)
            {
                error = exception;

                return false;
            }
        }

        public bool TryLoadFromFile(FileInfo file)
        {
            return TryLoadFromFile(file, out _);
        }

        public bool TryLoadFromFile(FileInfo file, out Exception? error)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            error = null;
            Exception? capturedError = null;

            bool TryLoad()
            {
                bool success = false;

                try
                {
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

                        success = TrySetCache(loadedData.Cache);
                    }
                }
                catch (Exception exception)
                {
                    capturedError = exception;

                    success = false;
                }

                return success;
            }

            try
            {
                var lockAcquired = DoWhileWriteLocked(TryLoad, out var success, DefaultTimeout);
                
                if (!lockAcquired || !success)
                {
                    error = capturedError;

                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                error = exception;

                return false;
            }
        }

        public ImmutableDictionary<string, CacheEntry> GetEntries()
        {
            var result = DoWhileReadLocked(_cache.GetEntries, out var dictionary, DefaultTimeout);

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
                var result = DoWhileWriteLocked(
                () =>
                {
                    if (Id != null)
                    {
                        Factory.Remove(Id);
                    }

                    if (disposing)
                    {
                        _cache?.Dispose();
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
            bool equals()
            {
                bool innerEquals()
                {
                    return _cache.Equals(other._cache) &&
                           _cache.Id.Equals(other._cache.Id);
                }

                return other != null &&
                       _cache != null &&
                       other.DoWhileReadLocked(innerEquals, out var innerAreEqual, DefaultTimeout) &&
                       innerAreEqual;
            }

            return DoWhileReadLocked(equals, out var areEqual, DefaultTimeout) && areEqual;
        }

        public override bool Equals(object? obj) => Equals(obj as ThreadSafeCache);

        public override int GetHashCode() => HashCode.Combine(_cache.GetHashCode(), Id);
        #endregion

        #region ICloneable

        public object Clone()
        {
            CacheNullException.ThrowIf(_cache == null, "Cannot clone a parially initialzied cache.");

            object clone()
            {
                return new ThreadSafeCache((ICacheBase<string, string>)_cache.Clone(),
                                           PersistenceEnabled,
                                           PersistenceStorageFile?.FullName ?? string.Empty,
                                           BlockedEntryBehavior,
                                           (CacheTypeRegister)TypeRegister.Clone());
            }

            var lockAquired = DoWhileReadLocked(clone, out var cache, DefaultTimeout);

            ReadLockException.ThrowIf(!lockAquired, "Could not acquire read lock while cloning object.");

            return cache;
        }
        #endregion
    }
}
