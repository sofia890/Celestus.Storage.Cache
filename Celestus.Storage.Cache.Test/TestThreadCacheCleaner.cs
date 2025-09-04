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
        var interval = CacheConstants.TimingDuration;
        using var cleaner = CacheCleanerHelper.GetCleaner(cleanerType, interval, out var cache);

        cleaner.RegisterCache(new(cache));

        long nowInTicks = DateTime.UtcNow.Ticks;

        const string KEY_1 = "Key1";
        CleanerHelper.AddEntryToCache(KEY_1, DateTime.UtcNow, cache);

        //
        // Act & Assert
        //
        Assert.IsFalse(cache.EntryRemoved.WaitOne(interval / 2));

        Assert.IsTrue(cache.EntryRemoved.WaitOne(interval * 2));

        Assert.AreEqual(1, cache.RemovedKeys.Count);
        Assert.AreEqual(KEY_1, cache.RemovedKeys.First());
    }
}
