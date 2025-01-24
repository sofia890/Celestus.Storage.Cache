namespace Celestus.Storage.Cache.Test.Model
{
    public static class ThreadTimeout
    {
        public static ReturnType DoWhileLocked<ReturnType>(ThreadCache cache, Func<ReturnType> action, int timeout = ThreadCache.NO_TIMEOUT)
        {
            return Thread.DoUntil(() => cache.Lock(),
                                  (cacheLock) => cacheLock.Dispose(),
                                  action,
                                  timeout);
        }
    }
}
