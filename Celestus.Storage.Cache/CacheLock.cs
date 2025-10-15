using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Celestus.Storage.Cache
{
    public class CacheLock : IDisposable
    {
        private bool _disposed = false;
        private bool _wasLockedFromStart = false;
        private Action _unlock;

        private CacheLock(bool wasReadLockedFromStart, Action unlock)
        {
            _wasLockedFromStart = wasReadLockedFromStart;
            _unlock = unlock;
        }

        public static bool TryReadLock(ReaderWriterLockSlim slimLock, TimeSpan timeout, [MaybeNullWhen(false)] out CacheLock cacheLock)
        {
            return TryLock(slimLock.IsReadLockHeld, slimLock.TryEnterReadLock, slimLock.ExitReadLock, timeout, out cacheLock);
        }

        public static bool TryWriteLock(ReaderWriterLockSlim slimLock, TimeSpan timeout, [MaybeNullWhen(false)] out CacheLock cacheLock)
        {
            return TryLock(slimLock.IsWriteLockHeld, slimLock.TryEnterWriteLock, slimLock.ExitWriteLock, timeout, out cacheLock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryLock(bool isAlreadyLocked, Func<TimeSpan, bool> tryEnterLock, Action exitLock, TimeSpan timeout, [MaybeNullWhen(false)] out CacheLock cacheLock)
        {
            if (isAlreadyLocked || tryEnterLock(timeout))
            {
                cacheLock = new CacheLock(isAlreadyLocked, exitLock);

                return true;
            }
            else
            {
                cacheLock = null;

                return false;
            }
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
                    if (!_wasLockedFromStart)
                    {
                        _unlock();
                    }
                }

                _disposed = true;
            }
        }
        #endregion
    }

}
