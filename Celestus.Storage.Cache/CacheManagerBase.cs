using Celestus.Exceptions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Celestus.Storage.Cache
{
    public class CacheTimeoutException(string Message) : TimeoutException(Message);
    public class LoadTimeoutException(string Message) : CacheTimeoutException(Message);
    public class CleanupTimeoutException(string Message) : CacheTimeoutException(Message);
    public class SetTimeoutException(string Message) : CacheTimeoutException(Message);
    public class SetFromFileTimeoutException(string Message) : CacheTimeoutException(Message);
    public class UpdateFromFileTimeoutException(string Message) : CacheTimeoutException(Message);
    public class PersistenceMismatchException(string Message) : Exception(Message);

    public abstract class CacheManagerBase<CacheIdType, CacheKeyType, CacheType> : IDisposable, ICacheManager<CacheIdType, CacheType>
        where CacheIdType : class
        where CacheKeyType : class
        where CacheType : CacheBase<CacheIdType, CacheKeyType>
    {
        private TimeSpan _lockTimeout = TimeSpan.FromMilliseconds(5000);

        protected readonly ReaderWriterLockSlim _lock = new();
        readonly protected Dictionary<CacheIdType, WeakReference<CacheType>> _caches = [];
        readonly protected CacheManagerCleaner<CacheIdType, CacheKeyType, CacheType> _factoryCleaner;
        private bool _isDisposed;

        public CacheManagerBase()
        {
            _factoryCleaner = new();
            _factoryCleaner.RegisterManager(new(this));
        }

        public bool TryLoad(CacheIdType id, [NotNullWhen(true)] out CacheType? cache)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            cache = default;

            if (_lock.TryEnterReadLock(_lockTimeout))
            {
                try
                {
                    bool result = false;

                    if (_caches.TryGetValue(id, out var cacheReference) &&
                        cacheReference.TryGetTarget(out cache))
                    {
                        result = !cache.IsDisposed;
                    }

                    return result;
                }
                finally
                {
                    _lock.ExitReadLock();
                }

            }
            else
            {
                throw new LoadTimeoutException("Could not lock resource for reading.");
            }
        }

        public CacheType GetOrCreateShared(CacheIdType id, bool persistenceEnabled = false, string persistenceStorageLocation = "", TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (TryLoad(id, out var cache))
            {
                Condition.ThrowIf<PersistenceMismatchException>(
                    persistenceEnabled != cache.PersistenceEnabled,
                    $"Inconsistent persistence configuration for ID '{id}'.");

                return cache;
            }
            else
            {
                CacheType cacheToTrack = (CacheType)Activator.CreateInstance(typeof(CacheType), [id, persistenceEnabled, persistenceStorageLocation])!;

                if (_lock.TryEnterWriteLock(timeout ?? _lockTimeout))
                {
                    try
                    {
                        _caches[id] = new(cacheToTrack);

                        _factoryCleaner.MonitorElement(cacheToTrack);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }

                    return cacheToTrack;
                }
                else
                {
                    throw new SetTimeoutException("Could not lock resource for writing.");
                }
            }
        }

        public CacheType? UpdateOrLoadSharedFromFile(FileInfo file, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (TryCreateFromFile(file) is not CacheType loadedCache)
            {
                return null;
            }
            else if (TryLoad(loadedCache.Id, out var cacheToUpdate) && cacheToUpdate != null)
            {
                using (loadedCache)
                {
                    Condition.ThrowIf<UpdateFromFileTimeoutException>(
                        !Update(loadedCache, cacheToUpdate, timeout),
                        "Could not lock resource for writing.");

                    return cacheToUpdate;
                }
            }
            else
            {
                WeakReference<CacheType> cache = new(loadedCache);

                if (_lock.TryEnterWriteLock(timeout ?? _lockTimeout))
                {
                    try
                    {
                        _caches[loadedCache.Id] = cache;

                        _factoryCleaner.MonitorElement(loadedCache);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }

                    return loadedCache;
                }
                else
                {
                    loadedCache.Dispose();

                    throw new SetFromFileTimeoutException("Could not lock resource for writing.");
                }
            }
        }

        public TimeSpan GetCleanupInterval()
        {
            return _factoryCleaner.GetCleanupInterval();
        }



        public void SetCleanupInterval(TimeSpan interval)
        {
            _factoryCleaner.SetCleanupInterval(interval);
        }

        public void SetLockTimeoutInterval(TimeSpan interval)
        {
            _lockTimeout = interval;
        }

        public void Remove(CacheIdType id)
        {
            Condition.ThrowIf<CleanupTimeoutException>(!_lock.TryEnterWriteLock(_lockTimeout),
                                                       "Could not lock resource for writing.");

            try
            {
                _caches.Remove(id);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool RemoveIfExpired(CacheIdType id)
        {
            Condition.ThrowIf<CleanupTimeoutException>(!_lock.TryEnterWriteLock(_lockTimeout),
                                                       "Could not lock resource for writing.");

            try
            {
                if (_caches[id].TryGetTarget(out var cache) && cache.IsDisposed)
                {
                    _caches.Remove(id);

                    return true;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return false;
        }

        protected abstract CacheType? TryCreateFromFile(FileInfo file);

        protected abstract bool Update(CacheType from, CacheType to, TimeSpan? timeout);

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _factoryCleaner.Dispose();
                    _lock.Dispose();
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
