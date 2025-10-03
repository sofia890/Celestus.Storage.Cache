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

        public static Cache? TryCreateFromFile(Uri path)
        {
            return Serialize.TryCreateFromFile<Cache>(path);
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
                    PersistenceStoragePath = new(path);
                }
                else if (storeToFile)
                {
                    PersistenceStoragePath = GetDefaultpersistenceEnabledPath(Id);
                }
                else
                {
                    PersistenceStoragePath = null;
                }

                if (PersistenceStoragePath != null)
                {
                    var file = new FileInfo(PersistenceStoragePath.AbsolutePath);

                    if (file.Exists && file.Length > 0)
                    {
                        CacheLoadException.ThrowIf(!TryLoadFromFile(PersistenceStoragePath), $"Could not load cache for key '{Id}'.");
                    }
                }
            }
        }

        public void HandlePersistenceEnabledFinalization()
        {
            if (PersistenceEnabled && !_persistenceEnabledHandled)
            {
                if (!File.Exists(PersistenceStoragePath.AbsolutePath))
                {
                    _ = Directory.CreateDirectory(PersistenceStoragePath.AbsolutePath);
                }

                CacheSaveException.ThrowIf(!TrySaveToFile(PersistenceStoragePath),
                                           $"Could not save cache for key '{Id}'.");

                _persistenceEnabledHandled = true;
            }
        }

        private static Uri GetDefaultpersistenceEnabledPath(string key)
        {
            string commonAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

            Uri filePath;

            if (appPath == null)
            {
                filePath = new($"{Directory.GetCurrentDirectory()}/{assemblyName}/{key}.json");
            }
            else
            {
                var file = new FileInfo(appPath);

                var appName = file.Name;

                filePath = new($"{commonAppDataPath}/{appName}/{assemblyName}/{key}.json");

                if (!CanWrite.Test(filePath))
                {
                    filePath = new Uri($"{file.DirectoryName}/{assemblyName}/{key}.json");
                }
            }

            NopersistenceEnabledPathException.ThrowIf(!CanWrite.Test(filePath), "Could not find any writeable path for application.");

            return filePath;
        }

        #region CacheBase<string, string>
        internal override Dictionary<string, CacheEntry> Storage { get; set; }

        [MemberNotNullWhen(true, nameof(PersistenceStoragePath))]
        public override bool PersistenceEnabled { get => PersistenceStoragePath != null; }

        public override Uri? PersistenceStoragePath { get; set; }

        private CacheCleanerBase<string, string>? _cleaner;
        internal override CacheCleanerBase<string, string> Cleaner
        {
            get => _cleaner!;
            set
            {
                _cleaner = value;
                _cleaner.RegisterCache(new(this));
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

        private static long GetExpiration(TimeSpan? duration = null)
        {
            long expiration = long.MaxValue;

            if (duration is TimeSpan timeDuration)
            {
                expiration = DateTime.UtcNow.Ticks + timeDuration.Ticks;
            }

            return expiration;
        }

        public override void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            Set(key, value, GetExpiration(duration));
        }

        public void Set<DataType>(string key, DataType value, long expiration)
        {
            Set(key, value, expiration, out var _);
        }

        public void Set<DataType>(string key, DataType value, long expiration, out CacheEntry entry)
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
            Set(key, value, duration);

            return true;
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
                var currentTimeInTicks = DateTime.UtcNow.Ticks;
                found = entry.Expiration >= currentTimeInTicks;

                if (entry.Data is DataType data)
                {
                    value = data;
                }
                else if (entry.Data == null)
                {
                    value = default;
                    found = true;
                }
                else
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

        public override bool TrySaveToFile(Uri path)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            Serialize.SaveToFile(this, path);

            return true;
        }

        public override bool TryLoadFromFile(Uri path)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            var loadedData = Serialize.TryCreateFromFile<Cache>(path);

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
