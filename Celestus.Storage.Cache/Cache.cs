using Celestus.Serialization;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(CacheJsonConverter))]
    public class Cache : IDisposable
    {
        private bool _disposed = false;

        internal Dictionary<string, CacheEntry> Storage { get; set; }

        internal CacheCleanerBase<string> Cleaner { get; private set; }

        public string Key { get; init; }

        internal Cache(
            string key,
            Dictionary<string, CacheEntry> storage,
            CacheCleanerBase<string> cleaner,
            bool removalRegistered = false)
        {
            Storage = storage;
            Cleaner = cleaner;

            if (!removalRegistered)
            {
                Cleaner.RegisterRemovalCallback(new(TryRemove));
            }

            Key = key;
        }

        public Cache(string key) : this(key, [], new CacheCleaner<string>())
        {
        }

        public Cache() : this(string.Empty)
        {
        }

        public Cache(CacheCleanerBase<string> cleaner, bool doNotSetRemoval = false) :
            this(string.Empty, [], cleaner, doNotSetRemoval)
        {
        }

        public void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            long expiration = long.MaxValue;

            if (duration is TimeSpan timeDuration)
            {
                expiration = DateTime.UtcNow.Ticks + timeDuration.Ticks;
            }

            Set(key, value, expiration);
        }

        public void Set<DataType>(string key, DataType value, long expiration)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            var entry = new CacheEntry(expiration, value);
            Storage[key] = entry;

            Cleaner.TrackEntry(ref entry, key);
        }

        public (bool result, DataType? data) TryGet<DataType>(string key)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            var currentTimeInTicks = DateTime.UtcNow.Ticks;

            if (!Storage.TryGetValue(key, out var entry))
            {
                return (false, default);
            }
            else if (entry.Expiration < currentTimeInTicks)
            {
                Cleaner.EntryAccessed(ref entry, key, currentTimeInTicks);

                return (false, default);
            }
            else if (entry.Data == default)
            {
                Cleaner.EntryAccessed(ref entry, key, currentTimeInTicks);

                return (true, default);
            }
            else if (entry.Data is not DataType data)
            {
                Cleaner.EntryAccessed(ref entry, key, currentTimeInTicks);

                return (false, default);
            }
            else
            {
                Cleaner.EntryAccessed(ref entry, key, currentTimeInTicks);

                return (true, data);
            }
        }

        public bool TryRemove(List<string> keys)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            bool anyRemoved = false;

            for (int i = 0; i < keys.Count; i++)
            {
                anyRemoved |= Storage.Remove(keys[i]);
            }

            return anyRemoved;
        }

        public void SaveToFile(Uri path)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            Serialize.SaveToFile(this, path);
        }

        public static Cache? TryCreateFromFile(Uri path)
        {
            return Serialize.TryCreateFromFile<Cache>(path);
        }

        public bool TryLoadFromFile(Uri path)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            var loadedData = Serialize.TryCreateFromFile<Cache>(path);

            if (loadedData == null)
            {
                return false;
            }
            else
            {
                Storage = loadedData.Storage;

                return true;
            }
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Cleaner.Dispose();
                    Storage.Clear();
                }

                _disposed = true;
            }
        }

        public bool IsDisposed => _disposed;
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
    }
}
