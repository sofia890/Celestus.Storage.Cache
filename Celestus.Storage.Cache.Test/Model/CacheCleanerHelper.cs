namespace Celestus.Storage.Cache.Test.Model
{
    public static class CacheCleanerHelper
    {
        public static CacheCleanerBase<string> GetCleaner(Type cleanerTypeToTest, int cleanupIntervalInMs, out object context)
        {
            if (cleanerTypeToTest == typeof(CacheCleaner<string>))
            {
                context = new object();

                return new CacheCleaner<string>(cleanupIntervalInMs);
            }

            Assert.AreEqual(cleanerTypeToTest, typeof(ThreadCacheCleaner<string>));

            var threadContext = new CacheCleanerThreadContext();
            context = threadContext;

            var threadCache = new ThreadCacheCleaner<string>(cleanupIntervalInMs);
            threadCache.RegisterCollection(new(threadContext.Storage));

            return threadCache;
        }
    }
}
