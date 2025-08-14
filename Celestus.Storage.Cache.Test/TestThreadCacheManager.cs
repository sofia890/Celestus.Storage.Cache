using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadCacheManager
{
    [TestMethod]
    public void VerifyThatGetOrCreateSharedCreatesNewCacheWithKey()
    {
        //
        // Arrange
        //
        ThreadCache.Factory.SetCleanupInterval(TimeSpan.FromMilliseconds(10));

        //
        // Act
        //
        const string CACHE_KEY = nameof(VerifyThatGetOrCreateSharedCreatesNewCacheWithKey);

        object Prepare(out long hash)
        {
            var originalCache = ThreadCache.Factory.GetOrCreateShared(CACHE_KEY);
            _ = originalCache.TrySet(CACHE_KEY, 15);

            hash = originalCache.GetHashCode();

            return originalCache;
        }

        var helper = new GarbageCollectionHelper<long>();
        var hashOriginal = helper.ActAndCollect(Prepare, out var released);

        //
        // Assert
        //
        using ThreadCache? newThreadCache = ThreadCache.Factory.GetOrCreateShared(CACHE_KEY);

        Assert.IsTrue(released);
        Assert.AreNotEqual(hashOriginal, newThreadCache.GetHashCode());
    }
}