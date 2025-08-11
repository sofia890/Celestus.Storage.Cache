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
        Assert.ThrowsException<ObjectDisposedException>(() => cache.ThreadLock());
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TrySaveToFile(new Uri("file://test")));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TrySet("new-key", "new-value"));
        Assert.ThrowsException<ObjectDisposedException>(() => cache.TryGet<string>("test-key"));
    }

    [TestMethod]
    public void VerifyThatDisposedCacheIsRemovedFromFactory()
    {
        //
        // Arrange
        //
        const string testKey = "disposal-test-key";
        var cache1 = ThreadCacheManager.GetOrCreateShared(testKey);

        // Verify it's loaded
        Assert.IsTrue(ThreadCacheManager.IsLoaded(testKey));

        //
        // Act
        //
        cache1.Dispose();

        //
        // Assert - Factory should create a new instance
        //
        using var cache2 = ThreadCacheManager.GetOrCreateShared(testKey);

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
        using var cache = new ThreadCache("actor-disposal-test", cleaningIntervalInMs: 1000);

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
        Thread.Sleep(ThreadCacheConstants.WAIT_FOR_THREAD_IN_MS);

        // No exceptions should be thrown during cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}