using Celestus.Exceptions;
using Celestus.Io;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Celestus.Storage.Cache
{
    public class CacheIoException(string message) : IOException(message)
    {
        public static void ThrowIf(bool condition, string message)
        {
            Condition.ThrowIf<CacheIoException>(condition, message);
        }
    }

    public class CacheLoadException(string message) : CacheIoException(message);
    public class CacheSaveException(string message) : CacheIoException(message);
    public class NopersistenceEnabledPathException(string message) : CacheIoException(message);

    public abstract class CacheBase<KeyType> : IDisposable, ICloneable
        where KeyType : notnull
    {
        public const int NO_TIMEOUT = -1;

        public KeyType Key { get; init; }
        internal abstract CacheCleanerBase<KeyType> Cleaner { get; set; }
        public abstract bool IsDisposed { get; }
        internal abstract Dictionary<KeyType, CacheEntry> Storage { get; set; }

        [MemberNotNullWhen(true, nameof(PersistenceStoragePath))]
        public virtual bool PersistenceEnabled { get => PersistenceStoragePath != null; }
        public abstract Uri? PersistenceStoragePath { get; set; }

        public CacheBase(KeyType key)
        {
            Key = key;
        }

        public abstract DataType Get<DataType>(string key);

        public abstract (bool result, DataType? data) TryGet<DataType>(string key);

        public abstract void Set<DataType>(string key, DataType value, TimeSpan? duration = null);

        public abstract bool TrySet<DataType>(string key, DataType value, TimeSpan? duration = null);

        public abstract bool TryRemove(KeyType key);

        public abstract bool TryRemove(KeyType[] key);

        public abstract bool TrySaveToFile(Uri path);

        public abstract bool TryLoadFromFile(Uri path);

        #region IDisposable
        public abstract void Dispose();
        #endregion

        #region ICloneable
        public abstract object Clone();
        #endregion
    }
}
