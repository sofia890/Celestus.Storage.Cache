using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    /// <summary>
    /// Thread safe cache cleaner.
    /// </summary>
    [JsonConverter(typeof(ThreadSafeCacheCleanerJsonConverter))]
    public partial class ThreadSafeCacheCleaner<CacheIdType, CacheKeyType>(TimeSpan interval) : CacheCleanerBase<CacheIdType, CacheKeyType>
        where CacheIdType : notnull
        where CacheKeyType : notnull
    {
        const int DEFAULT_INTERVAL_IN_MS = 60000;
        readonly ThreadSafeCacheCleanerActor<CacheIdType, CacheKeyType> _server = new(interval);

        public ThreadSafeCacheCleaner() : this(interval: TimeSpan.FromMilliseconds(DEFAULT_INTERVAL_IN_MS))
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

        public override TimeSpan GetCleaningInterval()
        {
            return _server.GetCleaningInterval();
        }

        public override void SetCleaningInterval(TimeSpan interval)
        {
            _server.SetCleaningInterval(interval);
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
            return new ThreadSafeCacheCleaner<CacheIdType, CacheKeyType>(interval);
        }

        ~ThreadSafeCacheCleaner()
        {
            Dispose(false);
        }
    }
}
