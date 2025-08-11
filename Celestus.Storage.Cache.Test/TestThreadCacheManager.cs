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
        ThreadCacheManager.SetCleanupInterval(TimeSpan.FromMilliseconds(10));

        //
        // Act
        //
        var tracked = new List<WeakReference>();

        var referrer = new List<ThreadCache>();

        long hashOriginal = 0;

        var cacheKey = nameof(VerifyThatGetOrCreateSharedCreatesNewCacheWithKey);
        tracked.Add(Weak.CreateReference(() =>
        {
            var originalCache = ThreadCacheManager.GetOrCreateShared(cacheKey);
            _ = originalCache.TrySet(cacheKey, 15);

            hashOriginal = originalCache.GetHashCode();

            referrer.Add(originalCache); return originalCache;
        }));

        // Run some code that is expected to release the references
        referrer.Clear();

        // No exceptions should be thrown during cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();

        //
        // Assert
        //
        using ThreadCache? newThreadCache = ThreadCacheManager.GetOrCreateShared(cacheKey);

        Assert.IsFalse(tracked.Any(o => o.IsAlive), "All objects should have been released");
        Assert.AreNotEqual(hashOriginal, newThreadCache.GetHashCode());
    }
}