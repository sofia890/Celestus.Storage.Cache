using Celestus.Exceptions;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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

    public interface CacheBase<CacheIdType, CacheKeyType> : IDisposable, ICloneable
        where CacheIdType : notnull
        where CacheKeyType : notnull
    {
        public const int NO_TIMEOUT = -1;

        public abstract CacheIdType Id { get; }
        public abstract CacheCleanerBase<CacheIdType, CacheKeyType> Cleaner { get; set; }
        public abstract bool IsDisposed { get; }
        internal abstract Dictionary<CacheKeyType, CacheEntry> Storage { get; set; }

        [MemberNotNullWhen(true, nameof(PersistenceStorageFile))]
        public abstract bool PersistenceEnabled { get; }
        public abstract FileInfo? PersistenceStorageFile { get; set; }

        public abstract DataType Get<DataType>(CacheKeyType key);

        public abstract bool TryGet<DataType>(CacheKeyType key, [MaybeNullWhen(false)] out DataType data);

        public abstract void Set<DataType>(CacheKeyType key, DataType value, TimeSpan? duration = null);

        public abstract bool TrySet<DataType>(CacheKeyType key, DataType value, TimeSpan? duration = null);

        public abstract bool TryRemove(CacheKeyType key);

        public abstract bool TryRemove(CacheKeyType[] key);

        public abstract bool TrySaveToFile(FileInfo file);

        public abstract bool TryLoadFromFile(FileInfo file);

        public abstract ImmutableDictionary<CacheKeyType, CacheEntry> GetEntries();
    }
}
