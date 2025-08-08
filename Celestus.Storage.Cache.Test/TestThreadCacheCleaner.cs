using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize] // Timing tests become unreliable when run in parallel.
public class TestThreadCacheCleaner
{
    const int SHORT_DELAY_IN_MS = 2;
    const int SHORT_INTERVAL_IN_MS = 10;
    const int LONG_INTERVAL_IN_MS = 20;
    const int VERY_LONG_INTERVAL_IN_MS = 200;

    [TestMethod]
    public void VerifyThatCleanupOnlyHappensAfterInterval()
    {
        //
        // Arrange
        //
        const int INTERVAL_IN_MS = VERY_LONG_INTERVAL_IN_MS;
        using var cleaner = new ThreadCacheCleaner<string>(cleanupIntervalInMs: INTERVAL_IN_MS);

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
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(LONG_INTERVAL_IN_MS));

        cleaner.EntryAccessed(ref entry_2, KEY_2);

        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(LONG_INTERVAL_IN_MS));

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(INTERVAL_IN_MS));

        Assert.AreEqual(1, removalTracker.RemovedKeys.Count);
        Assert.AreEqual(KEY_2, removalTracker.RemovedKeys.First());
    }

    [TestMethod]
    public void VerifyThatCleanupAutomaticallyHappensAfterInterval()
    {
        //
        // Arrange
        //
        using var cleaner = new ThreadCacheCleaner<string>(cleanupIntervalInMs: SHORT_INTERVAL_IN_MS);

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
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(SHORT_INTERVAL_IN_MS / 2));

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(SHORT_INTERVAL_IN_MS * 2));

        Assert.AreEqual(1, removalTracker.RemovedKeys.Count);
        Assert.AreEqual(KEY_1, removalTracker.RemovedKeys.First());
    }
}
