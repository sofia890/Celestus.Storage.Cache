using Celestus.Storage.Cache.Test.Model;
using System.Text.Json;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize] // Timing tests become unreliable when run in parallel.
public class TestCacheCleaners
{
    [TestMethod]
    [DataRow(typeof(CacheCleaner<string>))]
    [DataRow(typeof(ThreadCacheCleaner<string>))]
    public void VerifyThatExpiredElementsAreRemovedWhenNewEntryIsTracked(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, ThreadCacheConstants.LONG_INTERVAL_IN_MS, out var context);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        const string KEY_1 = "Key1";
        CleanerHelper.TrackNewEntry(cleaner, KEY_1, DateTime.UtcNow, context);

        //
        // Act & Assert
        //
        const string KEY_2 = "Key2";
        CleanerHelper.TrackNewEntry(cleaner, KEY_2, DateTime.UtcNow.AddDays(1), context);

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(ThreadCacheConstants.VERY_LONG_INTERVAL_IN_MS));
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
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, ThreadCacheConstants.LONG_INTERVAL_IN_MS, out var context);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        const string KEY = "Key";
        CleanerHelper.TrackNewEntry(cleaner, KEY, DateTime.UtcNow, context, out var entry);

        //
        // Act & Assert
        //
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(ThreadCacheConstants.SHORT_DELAY_IN_MS));

        cleaner.EntryAccessed(ref entry, KEY);

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne());
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(ThreadCacheConstants.SHORT_DELAY_IN_MS));

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
        const int INTERVAL_IN_MS = ThreadCacheConstants.LONG_INTERVAL_IN_MS;
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, INTERVAL_IN_MS, out var context);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        long nowInTicks = DateTime.UtcNow.Ticks;

        const string KEY_1 = "Key1";
        CleanerHelper.TrackNewEntry(cleaner, KEY_1, DateTime.UtcNow.AddDays(1), context);

        const string KEY_2 = "Key2";
        CleanerHelper.TrackNewEntry(cleaner, KEY_2, DateTime.UtcNow, context, out var entry_2);

        //
        // Act & Assert
        //
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(ThreadCacheConstants.SHORT_DELAY_IN_MS));

        cleaner.EntryAccessed(ref entry_2, KEY_2);

        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(ThreadCacheConstants.SHORT_DELAY_IN_MS * 2));

        cleaner.EntryAccessed(ref entry_2, KEY_2);

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(INTERVAL_IN_MS * 200));

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
        using var cleanerA = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, ThreadCacheConstants.SHORT_DELAY_IN_MS, out var contextA);

        using var stream = new MemoryStream();
        using Utf8JsonWriter writer = new(stream);
        cleanerA.WriteSettings(writer, new());
        writer.Flush();

        //
        // Act
        //
        const int LONG_INTERVAL_IN_MS = 500000;
        using var cleanerB = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, LONG_INTERVAL_IN_MS, out var contextB);

        Utf8JsonReader reader = new(stream.ToArray());
        cleanerB.ReadSettings(ref reader, new());

        RemovalTracker removalTracker = new();
        cleanerB.RegisterRemovalCallback(new(removalTracker.TryRemove));

        const string KEY = "Key";
        CleanerHelper.TrackNewEntry(cleanerB, KEY, DateTime.UtcNow, contextB, out var entry);

        ThreadHelper.SpinWait(ThreadCacheConstants.SHORT_DELAY_IN_MS);

        cleanerB.EntryAccessed(ref entry, KEY);

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
        using CacheCleanerBase<string> cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, 1000, out var context);

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
        using CacheCleanerBase<string> cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, 1000, out var context);

        string json = "{\"ExtraParameter\":\"500\",\"_cleanupIntervalInTicks\":500}";
        CleaningHelper.ReadSettings(cleaner, json);
    }
}
