namespace Celestus.Storage.Cache.Test.Model
{
    internal interface IDoWhileLocked
    {
        protected ReaderWriterLockSlim GetLock();

        public ReturnType DoWhileWriteLocked<ReturnType>(Func<ReturnType> action)
        {
            return ThreadHelper.DoUntil(
                () =>
                {
                    var slimLock = GetLock();
                    _ = slimLock.TryEnterWriteLock(CacheConstants.VeryShortDuration);
                    return slimLock;
                },
                (lockSlim) => lockSlim.ExitWriteLock(),
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
                    _ = slimLock.TryEnterReadLock(CacheConstants.VeryShortDuration);
                    return slimLock;
                },
                (lockSlim) => lockSlim.ExitReadLock(),
                action,
                CacheConstants.VeryLongDuration
            );
        }
    }
}
