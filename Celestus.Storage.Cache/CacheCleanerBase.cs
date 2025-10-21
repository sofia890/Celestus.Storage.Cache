namespace Celestus.Storage.Cache
{
    public abstract class CacheCleanerBase<CacheIdType, CacheKeyType> : IDisposable, ICloneable
        where CacheIdType : notnull
        where CacheKeyType : notnull
    {
        private bool _disposed = false;

        public CacheCleanerBase()
        {

        }

        public abstract void EntryAccessed(ref CacheEntry entry, CacheKeyType key);

        public abstract void EntryAccessed(ref CacheEntry entry, CacheKeyType key, DateTime when);

        public abstract void RegisterCache(WeakReference<ICacheBase<CacheIdType, CacheKeyType>> cache);

        public abstract void UnregisterCache();

        public abstract TimeSpan GetCleaningInterval();

        public abstract void SetCleaningInterval(TimeSpan interval);

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
                _disposed = true;
            }
        }

        public bool IsDisposed => _disposed;
        #endregion

        #region ICloneable
        public abstract object Clone();
        #endregion
    }
}
