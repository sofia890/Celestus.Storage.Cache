using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public class ThreadCacheCleaner<KeyType>(TimeSpan interval) : CacheCleanerBase<KeyType>
        where KeyType : notnull
    {
        const int DEFAULT_INTERVAL_IN_MS = 60000;
        readonly ThreadCacheCleanerActor<KeyType> _server = new(interval);

        public ThreadCacheCleaner() : this(interval: TimeSpan.FromMilliseconds(DEFAULT_INTERVAL_IN_MS))
        {

        }

        public override void EntryAccessed(ref CacheEntry entry, KeyType key)
        {
            // For performance cleanup only happens according to a periodic timer.
            ObjectDisposedException.ThrowIf(IsDisposed, this);
        }

        public override void EntryAccessed(ref CacheEntry entry, KeyType key, long timeInTicks)
        {
            // For performance cleanup only happens according to a periodic timer.
            ObjectDisposedException.ThrowIf(IsDisposed, this);
        }

        public override void RegisterCache(WeakReference<CacheBase<KeyType>> cache)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _ = _server.CleanerPort.Writer.TryWrite(new RegisterCacheInd<KeyType>(cache));
        }

        public override void ReadSettings(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _server.ReadSettings(ref reader);
        }

        public override void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _server.WriteSettings(writer);
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

        ~ThreadCacheCleaner()
        {
            Dispose(false);
        }
    }
}
