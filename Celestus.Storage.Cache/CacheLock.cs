using Celestus.Exceptions;
using System.Text.Json;

namespace Celestus.Storage.Cache
{
    public class CacheLock : IDisposable
    {
        private bool _disposed = false;
        private readonly ReaderWriterLockSlim _lock;

        public CacheLock(ReaderWriterLockSlim cacheLock, int timeoutInMs = ThreadCache.NO_TIMEOUT)
        {
            _lock = cacheLock;

            Condition.ThrowIf<TimeoutException>(!_lock.TryEnterWriteLock(timeoutInMs),
                                                $"Timed out while waiting to acquire a write lock on {nameof(ThreadCache)}.");
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_lock.IsWriteLockHeld)
                    {
                        _lock.ExitWriteLock();
                    }
                }

                _disposed = true;
            }
        }
        #endregion
    }

}
