using Newtonsoft.Json.Linq;
using System.Linq;
using System.Runtime.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;

namespace Celestus.Storage.Cache
{
    [Serializable]
    public class Cache : ISerializable, IEquatable<Cache>
    {
        private record CacheEntry(long Expiration, object? Data);

        readonly Dictionary<string, CacheEntry> _storage = [];

        public Cache()
        {

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

        #region ISerializable
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (var (key, value) in _storage)
            {
                var data = value.Data;

                if (data == null)
                {
                    continue;
                }

                info.AddValue(key, new Dictionary<string, object>()
                {
                    { "expiration", value.Expiration },
                    { "data", data },
                    { "type", data.GetType() }
                });
            }
        }

        protected internal Cache(SerializationInfo info, StreamingContext context)
        {
            foreach (var element in info)
            {
                if (element.Value is not JObject cacheEntry)
                {
                    continue;
                }
                else if (!long.TryParse($"{cacheEntry["expiration"]}", out var expiration))
                {
                    continue;
                }
                else if (Type.GetType($"{cacheEntry["type"]}") is not Type type)
                {
                    continue;
                }
                else if (cacheEntry["data"] is JToken serializedData)
                {
                    if (serializedData.ToObject(type) is not object data)
                    {
                        continue;
                    }
                    else
                    {
                        _storage.Add(element.Name, new(expiration, data));
                    }
                }
            }
        }
        #endregion

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
