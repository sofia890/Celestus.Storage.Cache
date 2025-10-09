using System.Text.Json;

namespace Celestus.Storage.Cache
{
    /// <summary>
    /// Thread safe cache cleaner.
    /// </summary>
    public class ThreadCacheCleaner<CacheIdType, CacheKeyType>(TimeSpan interval) : CacheCleanerBase<CacheIdType, CacheKeyType>
        where CacheIdType : notnull
        where CacheKeyType : notnull
    {
        const int DEFAULT_INTERVAL_IN_MS = 60000;
        readonly ThreadCacheCleanerActor<CacheIdType, CacheKeyType> _server = new(interval);

        public ThreadCacheCleaner() : this(interval: TimeSpan.FromMilliseconds(DEFAULT_INTERVAL_IN_MS))
        {

        }

        public override void EntryAccessed(ref CacheEntry entry, CacheKeyType key)
        {
            // For performance cleanup only happens according to a periodic timer.
            ObjectDisposedException.ThrowIf(IsDisposed, this);
        }

        public override void EntryAccessed(ref CacheEntry entry, CacheKeyType key, DateTime when)
        {
            // For performance cleanup only happens according to a periodic timer.
            ObjectDisposedException.ThrowIf(IsDisposed, this);
        }

        public override void RegisterCache(WeakReference<CacheBase<CacheIdType, CacheKeyType>> cache)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _ = _server.CleanerPort.Writer.TryWrite(new RegisterCacheInd<CacheIdType, CacheKeyType>(cache));
        }

        public override void UnregisterCache()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _ = _server.CleanerPort.Writer.TryWrite(new UnregisterCacheInd());
        }

        public override void Deserialize(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _server.ReadSettings(ref reader, options);
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _server.WriteSettings(writer, options);
        }

        public override void SetCleaningInterval(TimeSpan interval)
        {
            _ = _server.CleanerPort.Writer.TryWrite(new ResetInd(interval));
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _server.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        public override object Clone()
        {
            return new ThreadCacheCleaner<CacheIdType, CacheKeyType>(interval);
        }

        ~ThreadCacheCleaner()
        {
            Dispose(false);
        }
    }
}
