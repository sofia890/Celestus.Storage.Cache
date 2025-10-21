using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache.Test.Model
{
    [JsonConverter(typeof(CacheCleanerTesterJsonConverter))]
    internal class CacheCleanerTester : CacheCleanerBase<string, string>
    {
        public static Dictionary<Guid, CacheCleanerTester> Testers = [];

        public List<string> AccessedKeys { get; set; } = [];

        public WeakReference<IEnumerable<KeyValuePair<string, CacheEntry>>> StorageCollection { get; set; } = new([]);

        public bool SettingsReadCorrectly { get; set; } = false;

        public bool SettingsWritten { get; set; } = false;

        public Guid Guid { get; set; } = Guid.NewGuid();

        WeakReference<ICacheBase<string, string>>? _cacheReference;

        public CacheCleanerTester() : base()
        {
            lock (Testers)
            {
                Testers.Add(Guid, this);
            }
        }

        public override void EntryAccessed(ref CacheEntry entry, string key)
        {
            if (IsDisposed)
            {
                return;
            }

            AccessedKeys.Add(key);
        }

        public override void EntryAccessed(ref CacheEntry entry, string key, DateTime when)
        {
            if (IsDisposed)
            {
                return;
            }

            AccessedKeys.Add(key);
        }

        public override void RegisterCache(WeakReference<ICacheBase<string, string>> cache)
        {
            if (IsDisposed)
            {
                return;
            }

            _cacheReference = cache;
        }

        public override void UnregisterCache()
        {
            if (IsDisposed)
            {
                return;
            }

            _cacheReference = null;
        }

        public override TimeSpan GetCleaningInterval()
        {
            throw new NotImplementedException();
        }

        public override void SetCleaningInterval(TimeSpan interval)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (Testers)
                {
                    Testers.Remove(Guid);
                }

                // Clear collections to help with garbage collection
                AccessedKeys.Clear();
            }

            base.Dispose(disposing);
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
