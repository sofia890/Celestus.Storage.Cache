using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize] // Timing tests become unreliable when run in parallel.
public class TestCacheCleaners
{
    [TestMethod]
    [DataRow(typeof(CacheCleaner<string, string>))]
    // ThreadSafeCacheCleaner does not prune on accesses due to optimization.
    public void VerifyThatExpiredElementsAreRemovedWhenEntryIsAccessed(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, CacheConstants.LongDuration, out var cache);

        const string KEY = "Key";
        CleanerHelper.AddEntryToCache(KEY, TimeSpan.FromDays(1), cache, out var entry);

        //
        // Act & Assert
        //
        Assert.IsFalse(cache.EntryRemoved.WaitOne(CacheConstants.ShortDuration));

        cleaner.EntryAccessed(ref entry, KEY, DateTime.UtcNow.AddDays(1));

        Assert.IsTrue(cache.EntryRemoved.WaitOne(CacheConstants.ShortDuration));
        Assert.IsFalse(cache.EntryRemoved.WaitOne(CacheConstants.ShortDuration));

        Assert.AreEqual(1, cache.RemovedKeys.Count);
        Assert.AreEqual(KEY, cache.RemovedKeys.First());
    }

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string, string>))]
    [DataRow(typeof(ThreadSafeCacheCleaner<string, string>))]
    public void VerifyThatCleanupOnlyHappensAfterInterval(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        GC.Collect();
        GC.WaitForPendingFinalizers();

        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerTypeToTest, CacheConstants.VeryLongDuration, out var cache);

        long nowInTicks = DateTime.UtcNow.Ticks;

        const string KEY_1 = "Key1";
        CleanerHelper.AddEntryToCache(KEY_1, TimeSpan.FromDays(1), cache, out var entry_1);

        // First cleanup attempt happens here.
        cleaner.EntryAccessed(ref entry_1, KEY_1);

        const string KEY_2 = "Key2";
        CleanerHelper.AddEntryToCache(KEY_2, TimeSpan.Zero, cache, out var entry_2);

        var interval = CacheConstants.TimingDuration;
        cleaner.SetCleaningInterval(interval);

        //
        // Act & Assert
        //
        Assert.IsFalse(cache.EntryRemoved.WaitOne(interval * 0.25));

        cleaner.EntryAccessed(ref entry_2, KEY_2);

        Assert.IsFalse(cache.EntryRemoved.WaitOne(interval * 0.5));

        Thread.Sleep(interval);
        cleaner.EntryAccessed(ref entry_2, KEY_2);

        Assert.IsTrue(cache.EntryRemoved.WaitOne(interval * 0.5));

        Assert.AreEqual(1, cache.RemovedKeys.Count);
        Assert.AreEqual(KEY_2, cache.RemovedKeys.First());
    }

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string, string>))]
    [DataRow(typeof(ThreadSafeCacheCleaner<string, string>))]
    public void VerifyThatMissingIntervalCausesCrash(Type cleanerTypeToTest)
    {
        string json = "{\"parameter\":\"value\"}";
        Assert.ThrowsException<MissingValueJsonException>(() => CleaningHelper.Deserialize(json, cleanerTypeToTest));
    }

    [TestMethod]
    [DataRow(typeof(CacheCleaner<string, string>))]
    [DataRow(typeof(ThreadSafeCacheCleaner<string, string>))]
    public void VerifyThatUnknownParametersAreIgnored(Type cleanerTypeToTest)
    {
        //
        // Arrange
        //
        string json = "{\"ExtraParameter\":\"500\",\"CleanupInterval\":\"00:00:00.5\"}";
        CleaningHelper.Deserialize(json, cleanerTypeToTest);
    }
}
