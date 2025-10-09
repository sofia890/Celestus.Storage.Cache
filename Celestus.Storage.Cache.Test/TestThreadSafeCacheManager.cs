using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadSafeCacheManager
{
    [TestMethod]
    public void VerifyThatGetOrCreateSharedCreatesNewCacheWithKey()
    {
        ThreadSafeCache.Factory.SetCleanupInterval(TimeSpan.FromMilliseconds(10));

        const string CACHE_KEY = nameof(VerifyThatGetOrCreateSharedCreatesNewCacheWithKey);

        static object Prepare(out long hash)
        {
            var originalCache = ThreadSafeCache.Factory.GetOrCreateShared(CACHE_KEY);
            _ = originalCache.TrySet(CACHE_KEY, 15);
            hash = originalCache.GetHashCode();
            return originalCache;
        }

        var hashOriginal = GarbageCollectionHelper<long>.ActAndCollect(Prepare, out var released, CacheConstants.TimingDuration);

        using ThreadSafeCache? newThreadCache = ThreadSafeCache.Factory.GetOrCreateShared(CACHE_KEY);

        Assert.IsTrue(released);
        Assert.AreNotEqual(hashOriginal, newThreadCache.GetHashCode());
    }
}
