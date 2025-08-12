using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize] // Timing tests become unreliable when run in parallel.
public class TestThreadCacheCleaner
{
    [TestMethod]
    public void VerifyThatCleanupAutomaticallyHappensAfterInterval()
    {
        //
        // Arrange
        //
        var cleanerType = typeof(ThreadCacheCleaner<string>);
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerType, ThreadCacheConstants.LONG_INTERVAL_IN_MS, out var context);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        long nowInTicks = DateTime.UtcNow.Ticks;

        const string KEY_1 = "Key1";
        CleanerHelper.TrackNewEntry(cleaner, KEY_1, DateTime.UtcNow, context);

        //
        // Act
        //

        //
        // Assert
        //
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(ThreadCacheConstants.LONG_INTERVAL_IN_MS / 2));

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(ThreadCacheConstants.LONG_INTERVAL_IN_MS * 2));

        Assert.AreEqual(1, removalTracker.RemovedKeys.Count);
        Assert.AreEqual(KEY_1, removalTracker.RemovedKeys.First());
    }
}
