using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadSafeCacheDisposal
{
    [TestMethod]
    public void VerifyThatDoubleDisposalIsHandledGracefully()
    {
        var cache = new ThreadSafeCache("test-key");
        cache.Dispose();
        cache.Dispose();
    }

    [TestMethod]
    public void VerifyThatLockedOperationsThrowAfterDisposal()
    {
        var cache = new ThreadSafeCache("test-key");
        cache.Dispose();

        Assert.ThrowsException<ObjectDisposedException>(() => cache.TryGetWriteLock(out _));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TrySaveToFile(new FileInfo("file://test")));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TryLoadFromFile(new FileInfo("file:///temp")));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TrySet("new-key", "new-value"));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TryGet<string>("test-key", out _));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.Set("key", "value"));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.Get<string>("key"));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TryRemove(["key"]));
    }

    [TestMethod]
    public void VerifyThatDisposedCacheIsRemovedFromFactory()
    {
        const string testKey = "disposal-test-key";
        var cache1 = ThreadSafeCache.Factory.GetOrCreateShared(testKey);
        Assert.IsTrue(ThreadSafeCache.Factory.TryLoad(testKey, out _));
        cache1.Dispose();
        using var cache2 = ThreadSafeCache.Factory.GetOrCreateShared(testKey);
        Assert.IsFalse(ReferenceEquals(cache1, cache2));
    }

    [TestMethod]
    public void VerifyThatCleanerIsDisposedWhenThreadSafeCacheIsDisposed()
    {
        using var cleanerTester = new CacheCleanerTester();
        var cache = new ThreadSafeCache("test-key", cleanerTester);

        _ = cache.TrySet("test", "value");

        cache.Dispose();

        Assert.IsTrue(cleanerTester.IsDisposed);
    }

    [TestMethod]
    public void VerifyThatThreadSafeCacheDoesNotCrashDuringCleanup()
    {
        using var cache = new ThreadSafeCache("actor-disposal-test", cleaningInterval: CacheConstants.ShortDuration);

        _ = cache.TrySet("test-key", "test-value");

        cache.Dispose();

        Thread.Sleep(CacheConstants.ShortDuration);

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
