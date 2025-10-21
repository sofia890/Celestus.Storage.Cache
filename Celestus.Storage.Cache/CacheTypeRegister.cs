namespace Celestus.Storage.Cache
{
    /// <summary>
    /// Per-cache type filter register that controls which data types are allowed to be (de)serialized
    /// for a specific cache instance. This instance is intentionally kept out of any serialization
    /// process (the custom cache converters never emit it) to avoid persisting potentially sensitive
    /// type filtering configuration.
    /// </summary>
    public sealed class CacheTypeRegister : ICloneable
    {
        private readonly object _lock = new();
        private CacheTypeFilterMode _mode = CacheTypeFilterMode.Blacklist;
        private readonly HashSet<Type> _registered = [];

        public CacheTypeFilterMode Mode
        {
            get
            {
                lock (_lock)
                {
                    return _mode;
                }
            }

            set
            {
                lock (_lock)
                {
                    _mode = value;
                }
            }
        }

        public CacheTypeRegister() { }

        public CacheTypeRegister(CacheTypeFilterMode mode, IEnumerable<Type> types)
        {
            Mode = mode;

            foreach (var type in types)
            {
                Register(type);
            }
        }

        public void Register(Type type)
        {
            if (type is null)
            {
                return;
            }

            lock (_lock)
            {
                _ = _registered.Add(type);
            }
        }

        public void Unregister(Type type)
        {
            if (type is null)
            {
                return;
            }

            lock (_lock)
            {
                _ = _registered.Remove(type);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _registered.Clear();
            }
        }
        public bool IsAllowed(Type type)
        {
            lock (_lock)
            {
                return _mode switch
                {
                    CacheTypeFilterMode.Blacklist => !_registered.Contains(type),
                    CacheTypeFilterMode.Whitelist => _registered.Contains(type),
                    _ => true
                };
            }
        }

        public Type? Resolve(string? assemblyQualifiedName, out bool allowed)
        {
            allowed = false;
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName)) return null;
            var type = Type.GetType(assemblyQualifiedName, throwOnError: false, ignoreCase: false);
            if (type == null) return null;
            allowed = IsAllowed(type);
            return type;
        }

        public object Clone()
        {
            return new CacheTypeRegister(Mode, [.. _registered]);
        }
    }

    public enum CacheTypeFilterMode
    {
        Blacklist = 0,
        Whitelist = 1
    }

    public enum BlockedEntryBehavior
    {
        Throw = 0,
        Ignore = 1
    }

    public class BlockedCacheTypeException(Type blockedType, string context) : Exception($"Blocked cache type '{blockedType.FullName}' encountered during {context}.")
    {
        public Type BlockedType { get; } = blockedType;
    }
}
