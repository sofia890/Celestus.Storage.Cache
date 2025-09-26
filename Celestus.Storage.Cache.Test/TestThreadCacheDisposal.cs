using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadCacheDisposal
{
    [TestMethod]
    public void VerifyThatDoubleDisposalIsHandledGracefully()
    {
        //
        // Arrange
        //
        var cache = new ThreadCache("test-key");

        //
        // Act & Assert - Should not throw
        //
        cache.Dispose();
        cache.Dispose(); // Second disposal should not cause issues
    }

    [TestMethod]
    public void VerifyThatLockedOperationsThrowAfterDisposal()
    {
        //
        // Arrange
        //
        var cache = new ThreadCache("test-key");
        cache.Dispose();

        //
        // Act & Assert
        //
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TryGetWriteLock(out _));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TrySaveToFile(new Uri("file://test")));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TryLoadFromFile(new Uri("file:///temp")));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TrySet("new-key", "new-value"));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TryGet<string>("test-key"));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.Set("key", "value"));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.Get<string>("key"));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TryRemove(["key"]));
    }

    [TestMethod]
    public void VerifyThatDisposedCacheIsRemovedFromFactory()
    {
        //
        // Arrange
        //
        const string testKey = "disposal-test-key";
        var cache1 = ThreadCache.Factory.GetOrCreateShared(testKey);

        // Verify it's loaded
        Assert.IsTrue(ThreadCache.Factory.TryLoad(testKey, out _));

        //
        // Act
        //
        cache1.Dispose();

        //
        // Assert - Factory should create a new instance
        //
        using var cache2 = ThreadCache.Factory.GetOrCreateShared(testKey);

        Assert.IsFalse(ReferenceEquals(cache1, cache2));
    }

    [TestMethod]
    public void VerifyThatCleanerIsDisposedWhenThreadCacheIsDisposed()
    {
        //
        // Arrange
        //
        using var cleanerTester = new CacheCleanerTester();
        var cache = new ThreadCache("test-key", cleanerTester);

        // Set up some data to verify cleaner is working
        _ = cache.TrySet("test", "value");

        //
        // Act
        //
        cache.Dispose();

        //
        // Assert - Cleaner should be disposed and not respond to further operations
        //
        Assert.IsTrue(cleanerTester.IsDisposed);
    }

    [TestMethod]
    public void VerifyThatThreadCacheDoesNotCrashDuringCleanup()
    {
        //
        // Arrange
        //
        using var cache = new ThreadCache("actor-disposal-test", cleaningInterval: CacheConstants.ShortDuration);

        // Add some data to ensure the actor is working
        _ = cache.TrySet("test-key", "test-value");

        //
        // Act
        //
        cache.Dispose();

        //
        // Assert - Operations should fail gracefully after disposal
        //

        // Give some time for background task to shut down
        Thread.Sleep(CacheConstants.ShortDuration);

        // No exceptions should be thrown during cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}