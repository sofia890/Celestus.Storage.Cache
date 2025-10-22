using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Celestus.Storage.Cache.Test.Model
{
    internal class MockCache() : ICacheBase<string, string>
    {
        public AutoResetEvent EntryRemoved { get; private set; } = new(false);

        public List<string> RemovedKeys { get; private set; } = [];

        // Added backing storage so tests can insert entries that the cleaner actor will see.
        private readonly Dictionary<string, CacheEntry> _storage = new();

        #region CacheBase<string, string>
        private bool _disposed = false;
        public bool IsDisposed => _disposed;

        public ImmutableDictionary<string, CacheEntry> Storage { get => _storage.ToImmutableDictionary(); }

        private CacheCleanerBase<string, string>? _cleaner;

        public CacheCleanerBase<string, string> Cleaner
        {
            get => _cleaner!;
            set => _cleaner = value;
        }

        public BlockedEntryBehavior BlockedEntryBehavior
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException(); 
        }

        public CacheTypeRegister TypeRegister
        { 
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public FileInfo? PersistenceStorageFile
        {
            get => null;
            set => throw new NotImplementedException();
        }

        public string Id => throw new NotImplementedException();

        CacheCleanerBase<string, string> ICacheBase<string, string>.Cleaner { get => Cleaner; set => Cleaner = value; }

        public bool PersistenceEnabled => throw new NotImplementedException();

        public void Dispose()
        {
            _disposed = true;
            EntryRemoved.Dispose();
        }

        public CacheEntry GetEntry(string key)
        {
            return _storage[key];
        }

        public DataType Get<DataType>(string key)
        {
            throw new NotImplementedException();
        }

        public bool TryGet<DataType>(string key, [MaybeNullWhen(false)] out DataType data)
        {
            data = default;

            throw new NotImplementedException();
        }

        public void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            var expiration = DateTime.UtcNow.Add(duration ?? TimeSpan.FromDays(2));

            _storage.Add(key, new CacheEntry(expiration, value));
        }

        public bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            throw new NotImplementedException();
        }

        public bool TryLoadFromFile(FileInfo file)
        {
            throw new NotImplementedException();
        }

        public bool TryRemove(string[] keys)
        {
            foreach (var k in keys)
            {
                if (_storage.Remove(k))
                {
                    RemovedKeys.Add(k);
                }
            }

            if (RemovedKeys.Count >0)
            {
                EntryRemoved.Set();
            }

            return true;
        }

        public bool TrySaveToFile(FileInfo file)
        {
            throw new NotImplementedException();
        }

        internal ImmutableDictionary<string, CacheEntry> GetEntries()
        {
            return Storage.ToImmutableDictionary();
        }

        public bool TryRemove(string key)
        {
            throw new NotImplementedException();
        }

        ImmutableDictionary<string, CacheEntry> ICacheBase<string, string>.GetEntries()
        {
            return GetEntries();
        }
        #endregion


        #region ICloneable
        public object Clone()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
