using System.Diagnostics;

namespace Celestus.Storage.Cache.Test.Model
{
    public static class ThreadHelper
    {
        public static void SpinWait(int durationInMs)
        {
            SpinWait(TimeSpan.FromMilliseconds(durationInMs));
        }
        public static void SpinWait(TimeSpan duration)
        {
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < duration)
            {
                Thread.SpinWait(100);
            }
        }

        public static ReturnType DoUntil<DataType, ReturnType>(
            Func<DataType> initBackgroundThread,
            Action<DataType> cleanupBackgroundThread,
            Func<ReturnType> actionInThisThread,
            TimeSpan timeout)
        {
            ManualResetEvent done = new(false);
            ManualResetEvent backgroundThreadReady = new(false);

            Task.Run(() =>
            {
                var data = initBackgroundThread();
                _ = backgroundThreadReady.Set();

                _ = done.WaitOne(timeout);
                cleanupBackgroundThread(data);
            });

            backgroundThreadReady.WaitOne();
            var results = actionInThisThread();

            _ = done.Set();

            done.Dispose();
            backgroundThreadReady.Dispose();

            return results;
        }

        public static ReturnType DoWhileLocked<ReturnType>(ThreadCache cache, Func<ReturnType> action, TimeSpan timeout)
        {
            return DoUntil(() => _ = cache.ThreadLock(),
                           (cacheLock) => cacheLock.Dispose(),
                           action,
                           timeout);
        }
    }
}
