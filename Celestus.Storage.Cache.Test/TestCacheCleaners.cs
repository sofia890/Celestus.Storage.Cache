using Celestus.Storage.Cache.Test.Model;
using System.Text.Json;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize] // Timing tests become unreliable when run in parallel.
public class TestCacheCleaners
{
    [TestMethod]
    [DataRow(typeof(CacheCleaner<string>))]
    // ThreadCacheCleaner does not prune on new entries due to optimization.
    public void VerifyThatExpiredElementsAreRemovedWhenNewEntryIsTracked(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, CacheConstants.LongDuration, out var context);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        const string KEY_1 = "Key1";
        CleanerHelper.TrackNewEntry(cleaner, KEY_1, DateTime.UtcNow, context);

        //
        // Act & Assert
        //
        const string KEY_2 = "Key2";
        CleanerHelper.TrackNewEntry(cleaner, KEY_2, DateTime.UtcNow.AddDays(1), context);

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(CacheConstants.VeryLongDuration));
        Assert.AreEqual(1, removalTracker.RemovedKeys.Count);
        Assert.AreEqual(KEY_1, removalTracker.RemovedKeys.First());
    }

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string>))]
    // ThreadCacheCleaner does not prune on accesses due to optimization.
    public void VerifyThatExpiredElementsAreRemovedWhenEntryIsAccessed(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, CacheConstants.LongDuration, out var context);

        RemovalTracker removalTracker = new();
        cleaner.RegisterRemovalCallback(new(removalTracker.TryRemove));

        const string KEY = "Key";
        CleanerHelper.TrackNewEntry(cleaner, KEY, DateTime.UtcNow.AddDays(1), context, out var entry);

        //
        // Act & Assert
        //
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(CacheConstants.ShortDuration));

        cleaner.EntryAccessed(ref entry, KEY, long.MaxValue);

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(CacheConstants.ShortDuration));
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(CacheConstants.ShortDuration));

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
        var intervalInMs = CacheConstants.LongDuration;
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, intervalInMs, out var context);

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
        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(CacheConstants.ShortDuration));

        cleaner.EntryAccessed(ref entry_2, KEY_2);

        Assert.IsFalse(removalTracker.EntryRemoved.WaitOne(CacheConstants.ShortDuration * 2));

        Thread.Sleep(intervalInMs);
        cleaner.EntryAccessed(ref entry_2, KEY_2);

        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(CacheConstants.ShortDuration));

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
        using var cleanerA = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, CacheConstants.ShortDuration, out var contextA);

        using var stream = new MemoryStream();
        using Utf8JsonWriter writer = new(stream);
        cleanerA.WriteSettings(writer, new());
        writer.Flush();

        //
        // Act
        //
        using var cleanerB = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, CacheConstants.VeryLongDuration, out var contextB);

        Utf8JsonReader reader = new(stream.ToArray());
        cleanerB.ReadSettings(ref reader, new());

        RemovalTracker removalTracker = new();
        cleanerB.RegisterRemovalCallback(new(removalTracker.TryRemove));

        const string KEY = "Key";
        CleanerHelper.TrackNewEntry(cleanerB, KEY, DateTime.UtcNow, contextB, out var entry);

        ThreadHelper.SpinWait(CacheConstants.ShortDuration);

        cleanerB.EntryAccessed(ref entry, KEY);

        //
        // Assert
        //
        Assert.IsTrue(removalTracker.EntryRemoved.WaitOne(CacheConstants.VeryLongDuration / 2));
    }

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string>))]
    [DataRow(typeof(ThreadCacheCleaner<string>))]
    public void VerifyThatMissingIntervalCausesCrash(Type cleanerTypeToTest)
    {
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, CacheConstants.ShortDuration, out var context);

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
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, CacheConstants.ShortDuration, out var context);

        string json = "{\"ExtraParameter\":\"500\",\"_cleanupIntervalInTicks\":500}";
        CleaningHelper.ReadSettings(cleaner, json);
    }
}
