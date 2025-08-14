namespace Celestus.Storage.Cache.Test.Model
{
    internal static class CacheCleanerHelper
    {
        public static CacheCleanerBase<string> GetCleaner(Type cleanerTypeToTest, TimeSpan interval, out CacheCleanerContext context)
        {
            context = new CacheCleanerContext();

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

            cleaner.RegisterCollection(new(context.Storage));

            return cleaner;
        }
    }
}
