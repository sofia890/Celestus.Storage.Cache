using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize]
    public class TestCacheManager
    {
        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadSafeCache))]
        public void VerifyThatCacheManager(Type cacheType)
        {
            //
            // Arrange
            //
            CacheManagerHelper.SetCleanupInterval(cacheType, CacheConstants.ShortDuration);

            const string CACHE_KEY = nameof(VerifyThatCacheManager);
            const string ITEM_KEY = "a";

            object Prepare(out long hash)
            {
                ICacheBase<string, string> originalCache = CacheHelper.GetOrCreateShared(cacheType, CACHE_KEY);
                originalCache.Set(ITEM_KEY, 15);

                hash = originalCache.GetHashCode();

                return originalCache;
            }

            //
            // Act
            //
            var hashOriginal = GarbageCollectionHelper<long>.ActAndCollect(Prepare, out var released, CacheConstants.TimingDuration);

            Thread.Sleep(CacheConstants.ShortDuration * 2);

            //
            // Assert
            //
            ICacheBase<string, string> cacheNew = CacheHelper.GetOrCreateShared(cacheType, CACHE_KEY);
            var hashNew = cacheNew.GetHashCode();

            Assert.AreNotEqual(hashOriginal, hashNew);

            // Cleanup
            CacheManagerHelper.ResetCleanupInterval(cacheType);
        }
    }
}