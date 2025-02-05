namespace Celestus.Storage.Cache.Test.Model
{
    public static class CacheCleanerHelper
    {
        public static CacheCleanerBase<string> GetCleaner(Type cleanerTypeToTest, int cleanupIntervalInMs)
        {
            if (cleanerTypeToTest == typeof(CacheCleaner<string>))
            {
                return new CacheCleaner<string>(cleanupIntervalInMs);
            }

            Assert.AreEqual(cleanerTypeToTest, typeof(ThreadCacheCleaner<string>));

            return new ThreadCacheCleaner<string>(cleanupIntervalInMs);
        }
    }
}
