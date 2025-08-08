using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestCacheCleaner
{
    [TestMethod]
    public void VerifyThatCleanupOnlyHappensAfterInterval()
    {
        //
        // Arrange
        //
        const int INTERVAL_IN_MS = 4;
        var cleaner = new CacheCleaner<string>(cleanupIntervalInMs: INTERVAL_IN_MS);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        long nowInTicks = DateTime.UtcNow.Ticks;

        const string KEY_1 = "Key1";
        CleanerHelper.TrackNewEntry(cleaner, KEY_1, DateTime.UtcNow.AddDays(1));

        const string KEY_2 = "Key2";
        CleanerHelper.TrackNewEntry(cleaner, KEY_2, DateTime.UtcNow);

        ThreadHelper.SpinWait(INTERVAL_IN_MS + 1);

        //
        // Act & Assert
        //
        Assert.AreEqual(0, removalTracker.RemovedKeys.Count);

        const string KEY_3 = "Key3";
        CleanerHelper.TrackNewEntry(cleaner, KEY_3, DateTime.UtcNow, out var entry_3);

        Assert.AreEqual(1, removalTracker.RemovedKeys.Count);
        Assert.AreEqual(KEY_2, removalTracker.RemovedKeys.First());
    }
}
