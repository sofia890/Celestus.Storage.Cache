using Celestus.Exceptions;
using System.Diagnostics.CodeAnalysis;

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
    public class NoPersistencePathException(string message) : CacheIoException(message);
    public class PersistencePathNotWriteableException(string message) : CacheIoException(message);

    public abstract class CacheBase<CacheIdType, CacheKeyType> : IDisposable, ICloneable
        where CacheIdType : notnull
        where CacheKeyType : notnull
    {
        public const int NO_TIMEOUT = -1;

        public CacheIdType Id { get; init; }
        internal abstract CacheCleanerBase<CacheIdType, CacheKeyType> Cleaner { get; set; }
        public abstract bool IsDisposed { get; }
        internal abstract Dictionary<CacheKeyType, CacheEntry> Storage { get; set; }

        [MemberNotNullWhen(true, nameof(PersistenceStorageFile))]
        public virtual bool PersistenceEnabled { get => PersistenceStorageFile != null; }
        public abstract FileInfo? PersistenceStorageFile { get; set; }

        public CacheBase(CacheIdType id)
        {
            Id = id;
        }

        public abstract DataType Get<DataType>(CacheKeyType key);

        public abstract bool TryGet<DataType>(CacheKeyType key, [MaybeNullWhen(false)] out DataType data);

        public abstract void Set<DataType>(CacheKeyType key, DataType value, TimeSpan? duration = null);

        public abstract bool TrySet<DataType>(CacheKeyType key, DataType value, TimeSpan? duration = null);

        public abstract bool TryRemove(CacheKeyType key);

        public abstract bool TryRemove(CacheKeyType[] key);

        public abstract bool TrySaveToFile(FileInfo file);

        public abstract bool TryLoadFromFile(FileInfo file);

        #region IDisposable
        public abstract void Dispose();
        #endregion

        #region ICloneable
        public abstract object Clone();
        #endregion
    }
}
