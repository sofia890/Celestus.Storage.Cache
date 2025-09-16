namespace Celestus.Storage.Cache.Test.Model
{
    internal class MockCache(string key = "", bool persistent = false, string persistentStoragePath = "") :
        CacheBase<string>(key, persistent, persistentStoragePath)
    {
        public AutoResetEvent EntryRemoved { get; private set; } = new(false);

        public List<string> RemovedKeys { get; private set; } = [];

        private bool _disposed = false;

        #region CacheBase<string>
        public override bool IsDisposed => _disposed;

        internal override CacheCleanerBase<string> Cleaner => throw new NotImplementedException();

        internal override Dictionary<string, CacheEntry> Storage { get; set; } = [];

        public override void Dispose()
        {
            _disposed = true;
            EntryRemoved.Dispose();
        }

        public override DataType? Get<DataType>(string key) where DataType : default
        {
            throw new NotImplementedException();
        }
        public override (bool result, DataType? data) TryGet<DataType>(string key) where DataType : default
        {
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
        #endregion
    }
}
