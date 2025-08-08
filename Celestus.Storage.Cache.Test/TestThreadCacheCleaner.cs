using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize] // Timing tests become unreliable when run in parallel.
public class TestThreadCacheCleaner
{
    [TestMethod]
    public void VerifyThatCleanupOnlyHappensAfterInterval()
    {
        //
        // Arrange
        //
        using var cleaner = new ThreadCacheCleaner<string>(cleanupIntervalInMs: ThreadCacheConstants.VERY_LONG_INTERVAL_IN_MS);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        long nowInTicks = DateTime.UtcNow.Ticks;

        const string KEY_1 = "Key1";
        CleanerHelper.TrackNewEntry(cleaner, KEY_1, DateTime.UtcNow.AddDays(1));

        const string KEY_2 = "Key2";
        CleanerHelper.TrackNewEntry(cleaner, KEY_2, DateTime.UtcNow, out var entry_2);

        //
        // Act & Assert
        //
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(ThreadCacheConstants.LONG_INTERVAL_IN_MS));

        cleaner.EntryAccessed(ref entry_2, KEY_2);

        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(ThreadCacheConstants.LONG_INTERVAL_IN_MS));

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(ThreadCacheConstants.VERY_LONG_INTERVAL_IN_MS));

        Assert.AreEqual(1, removalTracker.RemovedKeys.Count);
        Assert.AreEqual(KEY_2, removalTracker.RemovedKeys.First());
    }

    [TestMethod]
    public void VerifyThatCleanupAutomaticallyHappensAfterInterval()
    {
        //
        // Arrange
        //
        using var cleaner = new ThreadCacheCleaner<string>(cleanupIntervalInMs: ThreadCacheConstants.LONG_INTERVAL_IN_MS);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new (removalTracker.TryRemove));

        long nowInTicks = DateTime.UtcNow.Ticks;

        const string KEY_1 = "Key1";
        CleanerHelper.TrackNewEntry(cleaner, KEY_1, DateTime.UtcNow);

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
