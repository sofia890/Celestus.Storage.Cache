using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Celestus.Storage.Cache.Test.Model
{
    internal class MockCache() : CacheBase<string, string>
    {
        public AutoResetEvent EntryRemoved { get; private set; } = new(false);

        public List<string> RemovedKeys { get; private set; } = [];



        #region CacheBase<string, string>
        private bool _disposed = false;
        public bool IsDisposed => _disposed;

        public Dictionary<string, CacheEntry> Storage { get; set; } = [];

        private CacheCleanerBase<string, string>? _cleaner;

        public CacheCleanerBase<string, string> Cleaner
        {
            get => _cleaner!;
            set => _cleaner = value;
        }

        public FileInfo? PersistenceStorageFile
        {
            get => null;
            set => throw new NotImplementedException();
        }

        public string Id => throw new NotImplementedException();

        CacheCleanerBase<string, string> CacheBase<string, string>.Cleaner { get => Cleaner; set => Cleaner = value; }

        public bool PersistenceEnabled => throw new NotImplementedException();

        public void Dispose()
        {
            _disposed = true;
            EntryRemoved.Dispose();
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
            throw new NotImplementedException();
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
            RemovedKeys.AddRange(keys);

            EntryRemoved.Set();

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

        ImmutableDictionary<string, CacheEntry> CacheBase<string, string>.GetEntries()
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
