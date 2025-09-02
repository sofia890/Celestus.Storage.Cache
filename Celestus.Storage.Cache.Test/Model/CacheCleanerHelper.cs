namespace Celestus.Storage.Cache.Test.Model
{
    internal static class CacheCleanerHelper
    {
        public static CacheCleanerBase<string> GetCleaner(Type cleanerTypeToTest, TimeSpan interval, out MockCache cache)
        {
            cache = new MockCache();

            CacheCleanerBase<string> cleaner;

            if (cleanerTypeToTest == typeof(CacheCleaner<string>))
            {
                cleaner = new CacheCleaner<string>(interval);
            }
            else
            {
                Assert.AreEqual(cleanerTypeToTest, typeof(ThreadCacheCleaner<string>));

                cleaner = new ThreadCacheCleaner<string>(interval);
            }

            cleaner.RegisterCache(new(cache));

            return cleaner;
        }
    }
}
