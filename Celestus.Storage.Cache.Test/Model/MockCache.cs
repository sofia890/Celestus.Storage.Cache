using System.Diagnostics.CodeAnalysis;

namespace Celestus.Storage.Cache.Test.Model
{
    internal class MockCache(string key = "") :
        CacheBase<string, string>(key)
    {
        public AutoResetEvent EntryRemoved { get; private set; } = new(false);

        public List<string> RemovedKeys { get; private set; } = [];

        private bool _disposed = false;

        private CacheCleanerBase<string, string>? _cleaner;

        #region CacheBase<string, string>
        public override bool IsDisposed => _disposed;

        internal override Dictionary<string, CacheEntry> Storage { get; set; } = [];

        internal override CacheCleanerBase<string, string> Cleaner
        {
            get => _cleaner!;
            set => _cleaner = value;
        }

        public override Uri? PersistenceStoragePath
        {
            get => null;
            set => throw new NotImplementedException();
        }

        public override void Dispose()
        {
            _disposed = true;
            EntryRemoved.Dispose();
        }

        public override DataType Get<DataType>(string key) where DataType : default
        {
            throw new NotImplementedException();
        }
        public override bool TryGet<DataType>(string key, [MaybeNullWhen(false)] out DataType data) where DataType : default
        {
            data = default;

            throw new NotImplementedException();
        }

        public override void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            throw new NotImplementedException();
        }

        public override bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            throw new NotImplementedException();
        }

        public override bool TryLoadFromFile(Uri path)
        {
            throw new NotImplementedException();
        }

        public override bool TryRemove(string[] keys)
        {
            RemovedKeys.AddRange(keys);

            EntryRemoved.Set();

            return true;
        }

        public override bool TrySaveToFile(Uri path)
        {
            throw new NotImplementedException();
        }
        #endregion


        #region ICloneable

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public override bool TryRemove(string key)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
