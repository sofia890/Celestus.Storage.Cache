namespace Celestus.Storage.Cache
{
    public abstract class CacheBase<KeyType> : IDisposable
    {
        public const int NO_TIMEOUT = -1;

        public string Key { get; init; }
        internal abstract CacheCleanerBase<KeyType> Cleaner { get; }
        public abstract bool IsDisposed { get; }

        public CacheBase(string key)
        {
            Key = key;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public abstract void Set<DataType>(string key, DataType value, TimeSpan? duration = null);

        public abstract DataType? Get<DataType>(string key)
            where DataType : class;
    }
}
