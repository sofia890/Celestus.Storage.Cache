using Celestus.Exceptions;
using Celestus.Io;
using Celestus.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(CacheJsonConverter))]
    public partial class Cache : CacheBase<string, string>, IDisposable
    {
        bool _persistenceEnabledHandled;

        public Cache(
            string id,
            Dictionary<string, CacheEntry> storage,
            CacheCleanerBase<string, string> cleaner,
            bool persistenceEnabled = false,
            string persistenceStorageLocation = "",
            bool persistenceLoadWhenCreated = true) : base(id)
        {
            HandlePersistenceEnabledInitialization(persistenceEnabled, persistenceStorageLocation, persistenceLoadWhenCreated);

            if (!PersistenceEnabled || Storage == null)
            {
                Storage = storage;
            }

            Cleaner = cleaner;
        }

        public static Cache? TryCreateFromFile(FileInfo file)
        {
            return Serialize.TryCreateFromFile<Cache>(file);
        }

        public Cache ToCache()
        {
            var clone = new Cache(Id)
            {
                Storage = Storage.ToDictionary(),
                Cleaner = (CacheCleanerBase<string, string>)Cleaner.Clone()
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

                if (!CanWrite.Test(filePath))
                {
                    if (file.DirectoryName != null)
                    {
                        filePath = new(Path.Combine([file.DirectoryName, assemblyName, $"{key}.json"]));
                    }
                }
            }

            NoPersistencePathException.ThrowIf(!CanWrite.Test(filePath), "Could not find any writeable path for application.");

            return filePath;
        }

        #region CacheBase<string, string>
        internal override Dictionary<string, CacheEntry> Storage { get; set; }

        [MemberNotNullWhen(true, nameof(PersistenceStorageFile))]
        public override bool PersistenceEnabled { get => PersistenceStorageFile != null; }

        public override FileInfo? PersistenceStorageFile { get; set; }

        private CacheCleanerBase<string, string>? _cleaner;
        internal override CacheCleanerBase<string, string> Cleaner
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

        public Cache(string id, bool persistenceEnabled = false, string persistenceStorageLocation = "") :
            this(id, [], new CacheCleaner<string, string>(), persistenceEnabled: persistenceEnabled, persistenceStorageLocation: persistenceStorageLocation)
        {
        }

        public Cache() :
            this(string.Empty,
                [],
                new CacheCleaner<string, string>(),
                persistenceEnabled: false,
                persistenceStorageLocation: "")
        {
        }

        public Cache(CacheCleanerBase<string, string> cleaner, bool persistenceEnabled = false, string persistenceStorageLocation = "") :
            this(string.Empty, [], cleaner, persistenceEnabled: persistenceEnabled, persistenceStorageLocation: persistenceStorageLocation)
        {
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

        public override void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            Set(key, value, GetExpiration(duration));
        }

        public void Set<DataType>(string key, DataType value, DateTime expiration)
        {
            Set(key, value, expiration, out var _);
        }

        public void Set<DataType>(string key, DataType value, DateTime expiration, out CacheEntry entry)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            entry = new CacheEntry(expiration, value);
            Storage[key] = entry;

            Cleaner.EntryAccessed(ref entry, key);
        }

        public void Set<DataType>(string key, DataType value, out CacheEntry entry, TimeSpan? duration = null)
        {
            Set(key, value, GetExpiration(duration), out entry);
        }

        public override bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null)
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

        public override DataType Get<DataType>(string key)
            where DataType : default
        {
            var result = TryGet<DataType>(key, out var value);

            Condition.ThrowIf<InvalidOperationException>(!result);

            return value!;
        }

        public override bool TryGet<DataType>(string key, [MaybeNullWhen(false)] out DataType value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            bool found = false;
            value = default;

            if (Storage.TryGetValue(key, out var entry))
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

        public override bool TryRemove(string[] keys)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            bool anyRemoved = false;

            for (int i = 0; i < keys.Length; i++)
            {
                anyRemoved |= Storage.Remove(keys[i]);
            }

            return anyRemoved;
        }

        public override bool TryRemove(string key)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            return Storage.Remove(key);
        }

        public override bool TrySaveToFile(FileInfo file)
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

        public override bool TryLoadFromFile(FileInfo file)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            var loadedData = Serialize.TryCreateFromFile<Cache>(file);

            if (loadedData == null)
            {
                return false;
            }
            else
            {
                Storage = loadedData.Storage.ToDictionary();

                return true;
            }
        }
        #endregion

        #region IDisposable
        private bool _disposed = false;

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Cache()
        {
            // Triggers persistence, saves state to file.
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                HandlePersistenceEnabledFinalization();

                if (disposing)
                {
                    Cleaner.Dispose();
                    Storage.Clear();
                }

                _disposed = true;
            }
        }

        public override bool IsDisposed => _disposed;
        #endregion

        #region IEquatable
        public bool Equals(Cache? other)
        {
            if (other == null || Storage.Count != other.Storage.Count)
            {
                return false;
            }

            foreach (var kvp in Storage)
            {
                if (!other.Storage.TryGetValue(kvp.Key, out var otherValue) ||
                    !kvp.Value.Equals(otherValue))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Cache);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var kvp in Storage.OrderBy(x => x.Key))
            {
                hash.Add(kvp.Key);
                hash.Add(kvp.Value);
            }

            return hash.ToHashCode();
        }
        #endregion

        #region ICloneable
        public override object Clone()
        {
            return ToCache();
        }
        #endregion
    }
}
