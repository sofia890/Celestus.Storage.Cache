namespace Celestus.Storage.Cache.Test
{
    internal class Thread
    {
        public static ReturnType DoUntil<DataType, ReturnType>(
            Func<DataType> initBackgroundThread,
            Action<DataType> cleanupBackgroundThread,
            Func<ReturnType> actionInThisThread,
            int timeout = ThreadCache.NO_TIMEOUT)
        {
            ManualResetEvent done = new(false);
            ManualResetEvent backgroundThreadReady = new(false);

            Task.Run(() =>
            {
                var data = initBackgroundThread();
                _ = backgroundThreadReady.Set();

                _ = done.WaitOne(timeout);
                cleanupBackgroundThread(data);

                done.Dispose();
            });

            backgroundThreadReady.WaitOne();
            var results = actionInThisThread();

            _ = done.Set();

            return results;
        }
    }
}
