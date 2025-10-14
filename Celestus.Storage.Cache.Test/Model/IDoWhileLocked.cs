namespace Celestus.Storage.Cache.Test.Model
{
    /// <summary>
    /// Provides helpers to execute an action while intentionally holding a lock in another thread.
    /// Tests depend on the lock REALLY being held. Previously we attempted to acquire a lock once
    /// and continued even if acquisition failed, causing race conditions (writer lock succeeded
    /// unexpectedly). We now spin until the lock is acquired to make tests deterministic.
    /// </summary>
    internal interface IDoWhileLocked
    {
        protected ReaderWriterLockSlim GetLock();

        public ReturnType DoWhileWriteLocked<ReturnType>(Func<ReturnType> action)
        {
            return ThreadHelper.DoUntil(
                () =>
                {
                    var slimLock = GetLock();
                    // Ensure the write lock is actually held before proceeding.
                    while (!slimLock.TryEnterWriteLock(CacheConstants.VeryShortDuration))
                    {
                        Thread.SpinWait(100);
                    }
                    return slimLock;
                },
                (lockSlim) =>
                {
                    if (lockSlim.IsWriteLockHeld)
                    {
                        lockSlim.ExitWriteLock();
                    }
                },
                action,
                CacheConstants.VeryLongDuration
            );
        }

        public ReturnType DoWhileReadLocked<ReturnType>(Func<ReturnType> action)
        {
            return ThreadHelper.DoUntil(
                () =>
                {
                    var slimLock = GetLock();
                    // Ensure the read lock is actually held before proceeding.
                    while (!slimLock.TryEnterReadLock(CacheConstants.VeryShortDuration))
                    {
                        Thread.SpinWait(100);
                    }
                    return slimLock;
                },
                (lockSlim) =>
                {
                    if (lockSlim.IsReadLockHeld)
                    {
                        lockSlim.ExitReadLock();
                    }
                },
                action,
                CacheConstants.VeryLongDuration
            );
        }
    }
}
