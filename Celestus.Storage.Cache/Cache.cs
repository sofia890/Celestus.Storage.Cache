using Celestus.Serialization;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(CacheJsonConverter))]
    public partial class Cache : CacheBase<string>, IDisposable, ICloneable
    {
        private bool _disposed = false;

        internal override Dictionary<string, CacheEntry> Storage { get; set; }

        internal override CacheCleanerBase<string> Cleaner { get; }

        public Cache(
            string key,
            Dictionary<string, CacheEntry> storage,
            CacheCleanerBase<string> cleaner,
            bool persistent = false,
            string persistentStorageLocation = "") : base(key, persistent, persistentStorageLocation)
        {
            // Not persistent or no persistent data loaded.
            if (!persistent || Storage == null)
            {
                Storage = storage;
            }

            Cleaner = cleaner;

            Cleaner.RegisterCache(new(this));
        }

        public Cache(string key, bool persistent = false, string persistentStorageLocation = "") :
            this(key, [], new CacheCleaner<string>(), persistent: persistent, persistentStorageLocation: persistentStorageLocation)
        {
        }

        public Cache() :
            this(string.Empty,
                [],
                new CacheCleaner<string>(),
                persistent: false,
                persistentStorageLocation: "")
        {
        }

        public Cache(CacheCleanerBase<string> cleaner, bool persistent = false, string persistentStorageLocation = "") :
            this(string.Empty, [], cleaner, persistent: persistent, persistentStorageLocation: persistentStorageLocation)
        {
        }

        private long GetExpiration(TimeSpan? duration = null)
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
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            Set(key, value, GetExpiration(duration));
        }

        public void Set<DataType>(string key, DataType value, long expiration)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

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
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            Set(key, value, GetExpiration(duration), out entry);
        }

        public override DataType? Get<DataType>(string key) 
            where DataType : default
        {
            var result = TryGet<DataType>(key);

            if (!result.result)
            {
                throw new InvalidOperationException();
            }
            else 
            {
                return result.data;
            }
        }

        public (bool result, DataType? data) TryGet<DataType>(string key)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            bool found = false;
            DataType? value = default;

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
                    found = value == null;
                }
                else
                {
                    found = false;
                }

                Cleaner.EntryAccessed(ref entry, key);
            }

            return (found, value);
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

        public override bool TrySaveToFile(Uri path)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            Serialize.SaveToFile(this, path);

            return true;
        }

        public static Cache? TryCreateFromFile(Uri path)
        {
            return Serialize.TryCreateFromFile<Cache>(path);
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

        public Cache ToCache()
        {
            var clone = new Cache(Key);
            clone.Storage = Storage.ToDictionary();

            return clone;
        }

        #region IDisposable
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
                HandlePersistentFinalization();

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

            // Compare each key-value pair efficiently
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

            // Sort keys to ensure consistent hash code regardless of insertion order
            foreach (var kvp in Storage.OrderBy(x => x.Key))
            {
                hash.Add(kvp.Key);
                hash.Add(kvp.Value);
            }

            return hash.ToHashCode();
        }
        #endregion

        #region ICloneable
        public object Clone()
        {
            return ToCache();
        }
        #endregion
    }
}
