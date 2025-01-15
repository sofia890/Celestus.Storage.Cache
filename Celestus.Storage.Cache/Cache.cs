using System.Text.Json.Serialization;

namespace Celestus.Storage.Cache
{
    [JsonConverter(typeof(CacheJsonConverter))]
    public class Cache(Dictionary<string, CacheEntry> storge)
    {
        #region Factory Pattern
        readonly static Dictionary<string, Cache> _caches = [];

        public static Cache CreateShared(string key)
        {
            lock (nameof(Cache))
            {
                if (_caches.TryGetValue(key, out var cache))
                {
                    return cache;
                }
                else
                {
                    cache = new Cache();

                    _caches[key] = cache;

                    return cache;
                }
            }
        }
        #endregion

        readonly internal Dictionary<string, CacheEntry> _storage = storge;

        public Cache() : this([])
        {

        }

        public void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            long expiraton = long.MaxValue;

            if (duration != null && duration != TimeSpan.Zero)
            {
                expiraton = DateTime.Now.Ticks + (duration?.Ticks ?? 0);
            }

            Set(key, value, expiraton);
        }

        public void Set<DataType>(string key, DataType value, long expiraton)
        {
            _storage[key] = new(expiraton, value);
        }
        public (bool result, DataType? data) TryGet<DataType>(string key)
        {
            if (!_storage.TryGetValue(key, out var entry))
            {
                return (false, default);
            }
            else if (entry.Expiration < DateTime.Now.Ticks)
            {
                return (false, default);
            }
            else if (entry.Data is not DataType data)
            {
                return (false, default);
            }
            else
            {
                return (true, data);
            }
        }

        #region IEquatable
        public bool Equals(Cache? other)
        {
            return other != null &&
                   _storage.Count == other._storage.Count &&
                   _storage.Intersect(other._storage).Count() == _storage.Count;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Cache);
        }

        public override int GetHashCode()
        {
            return _storage.Aggregate(0, (a, b) => HashCode.Combine(a, b));
        }
        #endregion
    }
}
