using Celestus.Storage.Cache.Test.Model;
using System.Text.Json;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize] // Timing tests become unreliable when run in parallel.
public class TestCacheCleaners
{
    const int SHORT_DELAY_IN_MS = 4;
    const int LONG_INTERVAL_IN_MS = 30;
    const int VERY_LONG_INTERVAL_IN_MS = 60000;

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string>))]
    [DataRow(typeof(ThreadCacheCleaner<string>))]
    public void VerifyThatExpiredElementsAreRemovedWhenNewEntryIsTracked(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        const int SHORT_INTERVAL_IN_MS = VERY_LONG_INTERVAL_IN_MS;
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, SHORT_INTERVAL_IN_MS);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        const string KEY_1 = "Key1";
        CleanerHelper.TrackNewEntry(cleaner, KEY_1, DateTime.UtcNow);

        //
        // Act & Assert
        //
        const string KEY_2 = "Key2";
        CleanerHelper.TrackNewEntry(cleaner, KEY_2, DateTime.UtcNow.AddDays(1));

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(SHORT_INTERVAL_IN_MS));
        Assert.AreEqual(1, removalTracker.RemovedKeys.Count);
        Assert.AreEqual(KEY_1, removalTracker.RemovedKeys.First());
    }

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string>))]
    [DataRow(typeof(ThreadCacheCleaner<string>))]
    public void VerifyThatExpiredElementsAreRemovedWhenEntryIsAccessed(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, LONG_INTERVAL_IN_MS);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        const string KEY = "Key";
        CleanerHelper.TrackNewEntry(cleaner, KEY, DateTime.UtcNow, out var entry);

        //
        // Act & Assert
        //
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(SHORT_DELAY_IN_MS));

        cleaner.EntryAccessed(ref entry, KEY);

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne());
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(SHORT_DELAY_IN_MS));

        Assert.AreEqual(1, removalTracker.RemovedKeys.Count);
        Assert.AreEqual(KEY, removalTracker.RemovedKeys.First());
    }

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string>))]
    [DataRow(typeof(ThreadCacheCleaner<string>))]
    public void VerifyThatCleanupOnlyHappensAfterInterval(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        const int INTERVAL_IN_MS = LONG_INTERVAL_IN_MS;
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, INTERVAL_IN_MS);

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
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(SHORT_DELAY_IN_MS));

        cleaner.EntryAccessed(ref entry_2, KEY_2);

        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(INTERVAL_IN_MS - SHORT_DELAY_IN_MS));

        cleaner.EntryAccessed(ref entry_2, KEY_2);

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(INTERVAL_IN_MS * 2));

        Assert.AreEqual(1, removalTracker.RemovedKeys.Count);
        Assert.AreEqual(KEY_2, removalTracker.RemovedKeys.First());
    }

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string>))]
    [DataRow(typeof(ThreadCacheCleaner<string>))]
    public void VerifyThatSerializationHandlesSettingsCorrectly(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, SHORT_DELAY_IN_MS);

        using var stream = new MemoryStream();
        using Utf8JsonWriter writer = new(stream);
        cleaner.WriteSettings(writer, new());
        writer.Flush();

        //
        // Act
        //
        const int LONG_INTERVAL_IN_MS = 500000;
        using var otherCleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, LONG_INTERVAL_IN_MS);

        Utf8JsonReader reader = new(stream.ToArray());
        otherCleaner.ReadSettings(ref reader, new());

        RemovalTracker removalTracker = new();
        otherCleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        const string KEY = "Key";
        CleanerHelper.TrackNewEntry(otherCleaner, KEY, DateTime.UtcNow, out var entry);

        ThreadHelper.SpinWait(SHORT_DELAY_IN_MS);

        otherCleaner.EntryAccessed(ref entry, KEY);

        //
        // Assert
        //
        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(LONG_INTERVAL_IN_MS / 2));
    }

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string>))]
    [DataRow(typeof(ThreadCacheCleaner<string>))]
    public void VerifyThatMissingIntervalCausesCrash(Type cleanerTypeToTest)
    {
        using CacheCleanerBase<string> cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, 1000);

        string json = "{\"ExtraParameter\":\"500\"}";
        Assert.ThrowsException<MissingValueJsonException>(() => CleaningHelper.ReadSettings(cleaner, json));
    }

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string>))]
    [DataRow(typeof(ThreadCacheCleaner<string>))]
    public void VerifyThatUnknownParametersAreIgnored(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        using CacheCleanerBase<string> cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, 1000);

        string json = "{\"ExtraParameter\":\"500\",\"_cleanupIntervalInTicks\":500}";
        CleaningHelper.ReadSettings(cleaner, json);
    }
}
