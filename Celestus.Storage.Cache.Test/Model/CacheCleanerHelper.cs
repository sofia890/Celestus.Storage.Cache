namespace Celestus.Storage.Cache.Test.Model
{
    internal static class CacheCleanerHelper
    {
        public static CacheCleanerBase<string, string> GetCleaner(Type cleanerTypeToTest, TimeSpan interval, out MockCache cache)
        {
            cache = new MockCache();

            CacheCleanerBase<string, string> cleaner;

            if (cleanerTypeToTest == typeof(CacheCleaner<string, string>))
            {
                cleaner = new CacheCleaner<string, string>(interval);
            }
            else
            {
                Assert.AreEqual(cleanerTypeToTest, typeof(ThreadCacheCleaner<string, string>));

                cleaner = new ThreadCacheCleaner<string, string>(interval);
            }

            cleaner.RegisterCache(new(cache));

            return cleaner;
        }
    }
}
