using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize]
    public class TestCacheManager
    {
        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadCache))]
        public void VerifyThatCacheManager(Type cacheType)
        {
            //
            // Arrange
            //
            Cache.Factory.SetCleanupInterval(TimeSpan.FromMilliseconds(ThreadCacheConstants.SHORT_DELAY_IN_MS));

            const string CACHE_KEY = nameof(VerifyThatCacheManager);
            const string ITEM_KEY = "a";

            object Prepare(out long hash)
            {
                CacheBase<string> originalCache = CacheHelper.GetOrCreateShared(cacheType, CACHE_KEY);
                originalCache.Set(ITEM_KEY, 15);

                hash = originalCache.GetHashCode();

                return originalCache;
            }

            var helper = new GarbageCollectionHelper<long>();

            //
            // Act
            //
            var hashOriginal = helper.ActAndCollect(Prepare, out var released);

            Thread.Sleep(ThreadCacheConstants.SHORT_DELAY_IN_MS * 2);

            //
            // Assert
            //
            CacheBase<string> cacheNew = CacheHelper.GetOrCreateShared(cacheType, CACHE_KEY);
            var hashNew = cacheNew.GetHashCode();

            Assert.AreNotEqual(hashOriginal, hashNew);

            // Cleanup
            Cache.Factory.SetCleanupInterval(TimeSpan.FromMilliseconds(CacheManagerCleaner<string, string, Cache>.DEFAULT_INTERVAL_IN_MS));
        }
    }
}