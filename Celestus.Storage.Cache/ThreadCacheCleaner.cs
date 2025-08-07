using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public class ThreadCacheCleaner<KeyType>(int cleanupIntervalInMs) : CacheCleanerBase<KeyType>
    {
        const int DEFAULT_INTERVAL_IN_MS = 60000;

        readonly ThreadCacheCleanerActor<KeyType> _server = new(cleanupIntervalInMs);

        public ThreadCacheCleaner() : this(DEFAULT_INTERVAL_IN_MS)
        {

        }

        public override void TrackEntry(ref CacheEntry entry, KeyType key)
        {
            if (IsDisposed)
            {
                return;
            }

            _ = _server.CleanerPort.Writer.TryWrite(new TrackEntryInd<KeyType>(key, entry));
        }

        public override void EntryAccessed(ref CacheEntry entry, KeyType key)
        {
            if (IsDisposed)
            {
                return;
            }

            var timeInTicks = DateTime.UtcNow.Ticks;
            _ = _server.CleanerPort.Writer.TryWrite(new EntryAccessedInd<KeyType>(key, timeInTicks));
        }

        public override void EntryAccessed(ref CacheEntry entry, KeyType key, long timeInTicks)
        {
            if (IsDisposed)
            {
                return;
            }

            _ = _server.CleanerPort.Writer.TryWrite(new EntryAccessedInd<KeyType>(key, timeInTicks));
        }

        public override void RegisterRemovalCallback(Func<List<KeyType>, bool> callback)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            _ = _server.CleanerPort.Writer.TryWrite(new RegisterRemovalCallbackInd<KeyType>(callback));
        }

        public override void ReadSettings(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            _server.ReadSettings(ref reader);
        }

        public override void WriteSettings(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

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
