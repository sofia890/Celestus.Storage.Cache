using System.Runtime.Serialization;

namespace Celestus.Storage.Cache
{
    [Serializable]
    public class Cache : ISerializable
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
        private record CacheEntry(long Expiration, object? Data);

        readonly Dictionary<string, CacheEntry> _storage;

        public Cache()
        {
            _storage = [];
        }

        public void Set<DataType>(string key, DataType value, TimeSpan? duration = null)
        {
            long expiraton = long.MaxValue;

            if (duration != null && duration != TimeSpan.Zero)
            {
                expiraton = DateTime.Now.Ticks + (duration?.Ticks ?? 0);
            }

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

        #region ISerializable
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Storage", _storage);
        }

        protected internal Cache(SerializationInfo info, StreamingContext context)
        {
            var data = info.GetValue("Storage", typeof(Dictionary<string, CacheEntry>));

            if (data is Dictionary<string, CacheEntry> storage)
            {
                _storage = storage;
            }
            else
            {
                throw new SerializationException($"Storage is not a {typeof(Dictionary<string, CacheEntry>)}.");
            }
        }
        #endregion
    }
}
