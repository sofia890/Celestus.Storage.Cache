using System.Diagnostics.CodeAnalysis;

namespace Celestus.Storage.Cache
{
    public class CacheTimeoutException(string Message) : TimeoutException(Message);
    public class LoadTimeoutException(string Message) : CacheTimeoutException(Message);
    public class CleanupTimeoutException(string Message) : CacheTimeoutException(Message);
    public class SetTimeoutException(string Message) : CacheTimeoutException(Message);
    public class SetFromFileTimeoutException(string Message) : CacheTimeoutException(Message);

    public abstract class CacheManagerBase<CacheKeyType, CacheType> : IDisposable, ICacheManager<CacheType>
        where CacheKeyType : class
        where CacheType : CacheBase<CacheKeyType>
    {
        private int _lockTimeoutInMs = 5000;

        protected readonly ReaderWriterLockSlim _lock = new();
        readonly protected Dictionary<string, WeakReference<CacheType>> _caches = [];
        readonly protected CacheManagerCleaner<string, CacheKeyType, CacheType> _factoryCleaner;
        private bool _isDisposed;

        public CacheManagerBase()
        {
            _factoryCleaner = new();
            _factoryCleaner.SetElementExpiredCallback(new(CacheExpired));
        }

        public bool TryLoad(string key, [NotNullWhen(true)] out CacheType? cache)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            cache = default;

            if (_lock.TryEnterReadLock(_lockTimeoutInMs))
            {
                bool result = false;

                if (_caches.TryGetValue(key, out var cacheReference) &&
                    cacheReference.TryGetTarget(out cache))
                {
                    result = !cache.IsDisposed;
                }

                _lock.ExitReadLock();

                return result;
            }
            else
            {
                throw new LoadTimeoutException("Could not lock resource for reading.");
            }
        }

        public CacheType GetOrCreateShared(string key = "")
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            var usedKey = (key.Length > 0) ? key : Guid.NewGuid().ToString();

            if (TryLoad(key, out var cache))
            {
                return cache;
            }
            else
            {
                if (_lock.TryEnterWriteLock(_lockTimeoutInMs))
                {
                    var createdCache = (CacheType)Activator.CreateInstance(typeof(CacheType), [usedKey])!;
                    _caches[usedKey] = new(createdCache);

                    _lock.ExitWriteLock();

                    _factoryCleaner.MonitorElement(createdCache);

                    return createdCache;
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
            else if(TryLoad(loadedCache.Key, out var cacheToUpdate) && cacheToUpdate != null)
            {
                using (loadedCache)
                {
                    if (Update(loadedCache, cacheToUpdate, timeout))
                    {
                        return cacheToUpdate;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                if (_lock.TryEnterWriteLock(_lockTimeoutInMs))
                {
                    _caches[loadedCache.Key] = new(loadedCache);

                    _lock.ExitWriteLock();

                    _factoryCleaner.MonitorElement(loadedCache);

                    return loadedCache;
                }
                else
                {
                    loadedCache.Dispose();

                    throw new SetFromFileTimeoutException("Could not lock resource for writing.");
                }
            }
        }

        internal void CacheExpired(string key)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            
            if (_lock.TryEnterWriteLock(_lockTimeoutInMs))
            {                
                _caches.Remove(key, out var cacheReference);

                _lock.ExitWriteLock();
            }
            else
            {
                throw new CleanupTimeoutException("Could not lock resource for writing.");
            }
        }

        public void SetCleanupInterval(TimeSpan interval)
        {
            _factoryCleaner.SetCleanupInterval(interval);
        }

        public void SetLockTimeoutInterval(TimeSpan interval)
        {
            _lockTimeoutInMs = interval.Milliseconds;
        }

        public void Remove(string key)
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
