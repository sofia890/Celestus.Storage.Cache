using Celestus.Exceptions;
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

    public abstract class CacheManagerBase<CacheKeyType, CacheType> : IDisposable, ICacheManager<CacheKeyType, CacheType>
        where CacheKeyType : class
        where CacheType : CacheBase<CacheKeyType>
    {
        private TimeSpan _lockTimeout = TimeSpan.FromMilliseconds(5000);

        protected readonly ReaderWriterLockSlim _lock = new();
        readonly protected Dictionary<CacheKeyType, WeakReference<CacheType>> _caches = [];
        readonly protected CacheManagerCleaner<CacheKeyType, CacheKeyType, CacheType> _factoryCleaner;
        private bool _isDisposed;

        public CacheManagerBase()
        {
            _factoryCleaner = new();
            _factoryCleaner.RegisterManager(new(this));
        }

        public bool TryLoad(CacheKeyType key, [NotNullWhen(true)] out CacheType? cache)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            cache = default;

            if (_lock.TryEnterReadLock(_lockTimeout))
            {
                try
                {
                    bool result = false;

                    if (_caches.TryGetValue(key, out var cacheReference) &&
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

        public CacheType GetOrCreateShared(CacheKeyType key, bool persistenceEnabled = false, string persistenceStorageLocation = "", TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (TryLoad(key, out var cache))
            {
                Condition.ThrowIf<PersistenceMismatchException>(
                    persistenceEnabled != cache.PersistenceEnabled,
                    $"Inconsistent persistence configuration for key '{key}'.");

                return cache;
            }
            else
            {
                CacheType cacheToTrack = (CacheType)Activator.CreateInstance(typeof(CacheType), [key, persistenceEnabled, persistenceStorageLocation])!;

                if (_lock.TryEnterWriteLock(timeout ?? _lockTimeout))
                {
                    try
                    {
                        _caches[key] = new(cacheToTrack);

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

        public CacheType? UpdateOrLoadSharedFromFile(Uri path, TimeSpan? timeout = null)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (TryCreateFromFile(path) is not CacheType loadedCache)
            {
                return null;
            }
            else if (TryLoad(loadedCache.Key, out var cacheToUpdate) && cacheToUpdate != null)
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
                        _caches[loadedCache.Key] = cache;

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

        internal void CacheExpired(CacheKeyType key)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            Condition.ThrowIf<CleanupTimeoutException>(!_lock.TryEnterWriteLock(_lockTimeout),
                                                       "Could not lock resource for writing.");

            try
            {
                _caches.Remove(key, out _);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void SetCleanupInterval(TimeSpan interval)
        {
            _factoryCleaner.SetCleanupInterval(interval);
        }

        public void SetLockTimeoutInterval(TimeSpan interval)
        {
            _lockTimeout = interval;
        }

        public void Remove(CacheKeyType key)
        {
            lock (this)
            {
                _caches.Remove(key);
            }
        }

        protected abstract CacheType? TryCreateFromFile(Uri path);

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
