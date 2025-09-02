using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public abstract class CacheCleanerBase<KeyType> : IDisposable
        where KeyType : notnull
    {
        private bool _disposed = false;

        public CacheCleanerBase()
        {

        }

        public abstract void EntryAccessed(ref CacheEntry entry, KeyType key);

        public abstract void EntryAccessed(ref CacheEntry entry, KeyType key, long timeInTicks);

        public abstract void RegisterCache(WeakReference<CacheBase<KeyType>> cache);

        public abstract void ReadSettings(ref Utf8JsonReader reader, JsonSerializerOptions options);

        public abstract void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options);

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
    }
}
