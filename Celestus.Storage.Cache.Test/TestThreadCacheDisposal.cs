using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadCacheDisposal
{
    [TestMethod]
    public void VerifyThatThreadCacheCanBeDisposed()
    {
        //
        // Arrange
        //
        var cache = new ThreadCache("test-key");

        //
        // Act & Assert
        //
        cache.Dispose();

        // Verify that operations fail gracefully after disposal
        Assert.IsFalse(cache.TrySet("key", "value"));
        Assert.AreEqual((false, default), cache.TryGet<string>("key"));
        Assert.IsFalse(cache.TryRemove(["key"]));
        Assert.IsFalse(cache.TryLoadFromFile(new Uri("file://test")));
    }

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
        Assert.ThrowsException<ObjectDisposedException>(() => cache.SaveToFile(new Uri("file://test")));
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
        var cache2 = ThreadCacheManager.GetOrCreateShared(testKey);
        Assert.IsFalse(ReferenceEquals(cache1, cache2));
    }

    [TestMethod]
    public void VerifyThatCleanerIsDisposedWhenThreadCacheIsDisposed()
    {
        //
        // Arrange
        //
        var cleanerTester = new CacheCleanerTester();
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
    public void VerifyThatUsingStatementWorksCorrectly()
    {
        //
        // Arrange & Act
        //
        bool operationSucceeded;
        using (var cache = new ThreadCache("using-test-key"))
        {
            operationSucceeded = cache.TrySet("key", "value");
        }

        //
        // Assert
        //
        Assert.IsTrue(operationSucceeded);
        
        // Verify key was removed from factory
        var newCache = ThreadCacheManager.GetOrCreateShared("using-test-key");
        Assert.AreEqual((false, default), newCache.TryGet<string>("key"));
        
        newCache.Dispose();
    }

    [TestMethod]
    public void VerifyThatThreadCacheCleanerActorIsProperlyDisposed()
    {
        //
        // Arrange
        //
        var cache = new ThreadCache("actor-disposal-test", cleaningIntervalInMs: 1000);

        // Add some data to ensure the actor is working
        _ = cache.TrySet("test-key", "test-value");

        //
        // Act
        //
        cache.Dispose();

        //
        // Assert - Operations should fail gracefully after disposal
        //
        Assert.IsFalse(cache.TrySet("new-key", "new-value"));
        Assert.AreEqual((false, default), cache.TryGet<string>("test-key"));

        // Give some time for background task to shut down
        Thread.Sleep(100);

        // No exceptions should be thrown during cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}