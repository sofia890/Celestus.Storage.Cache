using Celestus.Exceptions;
using Celestus.Io;
using Celestus.Serialization;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    /// <summary>
    /// Single threaded cache implementation with optional persistence to file.
    /// </summary>
    [JsonConverter(typeof(CacheJsonConverter))]
    public partial class Cache : ICacheBase<string, string>, IDisposable
    {
        private bool _persistenceEnabledHandled;

        public BlockedEntryBehavior BlockedEntryBehavior { get; set; } = BlockedEntryBehavior.Throw;

        public CacheTypeRegister TypeRegister { get; set; } = new();

        public Cache(
            string id,
            Dictionary<string, CacheEntry> storage,
            CacheCleanerBase<string, string> cleaner,
            BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw,
            bool persistenceEnabled = false,
            string persistenceStorageLocation = "",
            bool persistenceLoadWhenCreated = true)
        {
            Id = id;
            BlockedEntryBehavior = blockedEntryBehavior;

            HandlePersistenceEnabledInitialization(persistenceEnabled, persistenceStorageLocation, persistenceLoadWhenCreated);

            if (!PersistenceEnabled || _storage == null)
            {
                _storage = storage;
            }

            Cleaner = cleaner;
        }

        public Cache(
            string id,
            Dictionary<string, CacheEntry> storage,
            CacheCleanerBase<string, string> cleaner,
            bool persistenceEnabled,
            string persistenceStorageLocation,
            bool persistenceLoadWhenCreated) : this(id, storage, cleaner, BlockedEntryBehavior.Throw, persistenceEnabled, persistenceStorageLocation, persistenceLoadWhenCreated)
        {
        }

        public Cache(string id, bool persistenceEnabled = false, string persistenceStorageLocation = "") :
            this(id, [], new CacheCleaner<string, string>(), BlockedEntryBehavior.Throw, persistenceEnabled: persistenceEnabled, persistenceStorageLocation: persistenceStorageLocation)
        {
        }

        public Cache(string id, bool persistenceEnabled, string persistenceStorageLocation, BlockedEntryBehavior blockedEntryBehavior) :
            this(id, [], new CacheCleaner<string, string>(), blockedEntryBehavior, persistenceEnabled: persistenceEnabled, persistenceStorageLocation: persistenceStorageLocation)
        {
        }

        public Cache() :
            this(string.Empty,
                [],
                new CacheCleaner<string, string>(),
                BlockedEntryBehavior.Throw,
                persistenceEnabled: false,
                persistenceStorageLocation: "")
        {
        }

        public Cache(BlockedEntryBehavior blockedEntryBehavior) :
            this(string.Empty,
                [],
                new CacheCleaner<string, string>(),
                blockedEntryBehavior,
                persistenceEnabled: false,
                persistenceStorageLocation: "")
        {
        }

        public Cache(CacheCleanerBase<string, string> cleaner, bool persistenceEnabled = false, string persistenceStorageLocation = "", BlockedEntryBehavior blockedEntryBehavior = BlockedEntryBehavior.Throw) :
            this(string.Empty, [], cleaner, blockedEntryBehavior, persistenceEnabled: persistenceEnabled, persistenceStorageLocation: persistenceStorageLocation)
        {
        }

        public void Set<DataType>(string key, DataType value, DateTime expiration)
        {
            Set(key, value, expiration, out var _);
        }

        public void Set<DataType>(string key, DataType value, DateTime expiration, out CacheEntry entry)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            entry = new CacheEntry(expiration, value);
            _storage[key] = entry;

            Cleaner.EntryAccessed(ref entry, key);
        }

        public void Set<DataType>(string key, DataType value, out CacheEntry entry, TimeSpan? duration = null)
        {
            Set(key, value, GetExpiration(duration), out entry);
        }

        private static DateTime GetExpiration(TimeSpan? duration = null)
        {
            DateTime expiration = DateTime.MaxValue;

            if (duration is TimeSpan timeDuration)
            {
                expiration = DateTime.UtcNow.Add(timeDuration);
            }

            return expiration;
        }

        public static Cache? TryCreateFromFile(FileInfo file,
                                               BlockedEntryBehavior behaviourMode = BlockedEntryBehavior.Throw,
                                               CacheTypeFilterMode filterMode = CacheTypeFilterMode.Blacklist,
                                               IEnumerable<Type>? types = null)
        {
            var options = new JsonSerializerOptions();
            options.SetBlockedEntryBehavior(behaviourMode);
            options.SetCacheTypeRegister(new(filterMode, types ?? []));

            var loaded = Serialize.TryCreateFromFile<Cache>(file);

            return loaded;
        }

        /// <returns>Shallow clone of the cache.</returns>
        public Cache ToCache()
        {
            var clone = new Cache(Id)
            {
                _storage = _storage.ToDictionary(),
                Cleaner = (CacheCleanerBase<string, string>)Cleaner.Clone(),
                BlockedEntryBehavior = BlockedEntryBehavior,
                TypeRegister = (CacheTypeRegister)TypeRegister.Clone(),
                PersistenceStorageFile = PersistenceStorageFile
            };

            return clone;
        }

        public void HandlePersistenceEnabledInitialization(bool storeToFile, string? path, bool loadFromFile)
        {
            if (storeToFile && loadFromFile)
            {
                if (storeToFile && path?.Length > 0)
                {
                    PersistenceStorageFile = new(path);
                }
                else if (storeToFile)
                {
                    PersistenceStorageFile = GetDefaultPersistencePath(Id);

                    PersistencePathNotWriteableException.ThrowIf(!CanWrite.Test(PersistenceStorageFile), "Cannot write to provided path.");
                }
                else
                {
                    PersistenceStorageFile = null;
                }

                if (PersistenceStorageFile != null &&
                    PersistenceStorageFile.Exists &&
                    PersistenceStorageFile.Length > 0)
                {
                    CacheLoadException.ThrowIf(!TryLoadFromFile(PersistenceStorageFile), $"Could not load cache for key '{Id}'.");
                }
            }
        }

        public void HandlePersistenceEnabledFinalization()
        {
            if (PersistenceEnabled && !_persistenceEnabledHandled)
            {
                if (!PersistenceStorageFile.Exists)
                {
                    PersistenceStorageFile.Directory?.Create();
                }

                CacheSaveException.ThrowIf(!TrySaveToFile(PersistenceStorageFile),
                                           $"Could not save cache for key '{Id}'.");

                _persistenceEnabledHandled = true;
            }
        }

        private void QueuePersistenceOnBackgroundThread()
        {
            if (!PersistenceEnabled || _persistenceEnabledHandled)
            {
                return;
            }

            // Copy reference needed inside thread (no locking here since object is unreachable except by finalizer).
            var cache = this;

            try
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        cache.HandlePersistenceEnabledFinalization();
                    }
                    catch
                    {
                        // Swallow exceptions in background persistence during finalization.
                    }
                })
                {
                    IsBackground = true,
                    Name = "CachePersistenceFinalizer"
                };

                thread.Start();
            }
            catch
            {
                // If we cannot start a background thread, try to persist on the finalizer thread.
                try
                {
                    cache.HandlePersistenceEnabledFinalization();
                }
                catch
                {
                    // Swallow exceptions in background persistence during finalization.
                }
            }
        }

        private static FileInfo GetDefaultPersistencePath(string key)
        {
            string commonAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? nameof(Celestus.Storage.Cache);

            FileInfo filePath;

            if (appPath == null)
            {
                filePath = new(Path.Combine([Directory.GetCurrentDirectory(), assemblyName, $"{key}.json"]));
            }
            else
            {
                var file = new FileInfo(appPath);

                var appName = file.Name;

                filePath = new(Path.Combine([commonAppDataPath, appName, assemblyName, $"{key}.json"]));

                if (!CanWrite.Test(filePath) && file.DirectoryName != null)
                {
                    filePath = new(Path.Combine([file.DirectoryName, assemblyName, $"{key}.json"]));
                }
            }


            NoPersistencePathException.ThrowIf(!CanWrite.Test(filePath), "Could not find any writeable path for application.");

            return filePath;
        }

        #region CacheBase<string, string>
        public string Id { get; init; }

        private Dictionary<string, CacheEntry> _storage;
        public ImmutableDictionary<string, CacheEntry> Storage { get => _storage.ToImmutableDictionary(); }

        [MemberNotNullWhen(true, nameof(PersistenceStorageFile))]
        public bool PersistenceEnabled { get => PersistenceStorageFile != null; }

        public FileInfo? PersistenceStorageFile { get; set; }

        private CacheCleanerBase<string, string>? _cleaner;
        public CacheCleanerBase<string, string> Cleaner
        {
            get => _cleaner!;
            set
            {
                if (_cleaner != value)
                {
                    _cleaner?.UnregisterCache();

                    _cleaner = value;
                    _cleaner.RegisterCache(new(this));
                }
            }
        }

        public void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            Set(key, value, GetExpiration(duration));
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            try
            {
                Set(key, value, duration);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public DataType Get<DataType>(string key)
        {
            var result = TryGet<DataType>(key, out var value);

            Condition.ThrowIf<InvalidOperationException>(!result);

            return value!;
        }

        public bool TryGet<DataType>(string key, [MaybeNullWhen(false)] out DataType value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            bool found = false;
            value = default;

            if (_storage.TryGetValue(key, out var entry))
            {
                var currentTime = DateTime.UtcNow;
                found = entry.Expiration > currentTime;

                if (entry.Data is DataType data)
                {
                    value = data;
                }
                else if (entry.Data != null)
                {
                    found = false;
                }

                Cleaner.EntryAccessed(ref entry, key);
            }

            return found;
        }

        public bool TryRemove(string[] keys)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            bool anyRemoved = false;

            for (int i = 0; i < keys.Length; i++)
            {
                anyRemoved |= _storage.Remove(keys[i]);
            }

            return anyRemoved;
        }

        public bool TryRemove(string key)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return _storage.Remove(key);
        }

        public bool TrySaveToFile(FileInfo file)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            try
            {
                Serialize.SaveToFile(this, file);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryLoadFromFile(FileInfo file)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            var options = new JsonSerializerOptions();
            options.SetBlockedEntryBehavior(BlockedEntryBehavior);
            options.SetCacheTypeRegister(TypeRegister);

            var loadedData = Serialize.TryCreateFromFile<Cache>(file, options);

            if (loadedData == null)
            {
                return false;
            }
            else
            {
                _storage = loadedData._storage.ToDictionary();

                return true;
            }
        }

        public ImmutableDictionary<string, CacheEntry> GetEntries() => _storage.ToImmutableDictionary();

        #endregion

        #region IDisposable
        private bool _disposed = false;
        public bool IsDisposed => _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Cache() => Dispose(false);
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (Id != null)
                    {
                        Factory.Remove(Id);
                    }

                    HandlePersistenceEnabledFinalization();
                    Cleaner.Dispose();
                    _storage.Clear();
                }
                else
                {
                    QueuePersistenceOnBackgroundThread();
                }

                _disposed = true;
            }
        }
        CacheCleanerBase<string, string> ICacheBase<string, string>.Cleaner { get => Cleaner; set => Cleaner = value; }
        #endregion

        #region IEquatable
        public bool Equals(Cache? other)
        {
            if (other == null || _storage.Count != other._storage.Count)
            {
                return false;
            }

            foreach (var kvp in _storage)
            {
                if (!other._storage.TryGetValue(kvp.Key, out var otherValue) ||
                    !kvp.Value.Equals(otherValue))
                {
                    return false;
                }
            }

            return Id == other.Id &&
                   PersistenceEnabled == other.PersistenceEnabled &&
                   PersistenceStorageFile == other.PersistenceStorageFile &&
                   BlockedEntryBehavior == other.BlockedEntryBehavior;
        }

        public override bool Equals(object? obj) => Equals(obj as Cache);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(BlockedEntryBehavior);

            foreach (var kvp in _storage.OrderBy(x => x.Key))
            {
                hash.Add(kvp.Key);
                hash.Add(kvp.Value);
            }

            return hash.ToHashCode();
        }
        #endregion

        #region ICloneable
        /// <returns>Shallow clone of the cache.</returns>
        public object Clone() => ToCache();
        #endregion
    }
}
