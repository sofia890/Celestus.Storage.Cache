using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize] // Timing tests become unreliable when run in parallel.
public class TestThreadSafeCacheCleaning
{
    [TestMethod]
    public void VerifyThatCleanerIsInformedWhenEntriesAreAccessed()
    {
        using var cleanerTester = new CacheCleanerTester();
        using var cache = new ThreadSafeCache(cleanerTester);

        const string KEY_1 = "Hamster";
        const bool VALUE_1 = true;
        _ = cache.TrySet(KEY_1, VALUE_1);
        _ = cache.TryGet<bool>(KEY_1, out _);

        Assert.AreEqual(2, cleanerTester.AccessedKeys.Count);
        Assert.AreEqual(KEY_1, cleanerTester.AccessedKeys[0]);
    }

    [TestMethod]
    public void VerifyThatCleanerSerializationIsConnected()
    {
        using var cleanerTester = new CacheCleanerTester();
        using var cache = new ThreadSafeCache(cleanerTester);

        using var tempFile = new TempFile();
        _ = cache.TrySaveToFile(tempFile.Info);

        using ThreadSafeCache? loadedCache = ThreadSafeCache.TryCreateFromFile(tempFile.Info);

        Assert.IsTrue(cleanerTester.SettingsWritten);
        Assert.IsTrue(cleanerTester.SettingsReadCorrectly);
    }

    [TestMethod]
    public void VerifyThatCacheFreesUpMemory()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var interval = CacheConstants.TimingDuration;
        using var cache = new ThreadSafeCache(interval);

        var keys = new SerialKeys();
        var firstKey = keys.Next();
        _ = cache.TrySet(firstKey, ElementHelper.CreateSmallArray(), TimeSpan.FromDays(1));

        using var tempFile1 = new TempFile();
        _ = cache.TrySaveToFile(tempFile1.Info);

        const int NROF_KEYS = 1000;
        for (int i = 0; i < NROF_KEYS; i++)
        {
            Assert.IsTrue(cache.TrySet(keys.Next(), ElementHelper.CreateSmallArray(), CacheConstants.ShortDuration));
        }

        bool LastKeyNoLongerInCacheOrExpired() => cache.Cache.Storage.Count() == 1;

        var cleaned = ThreadHelper.DoPeriodicallyUntil(LastKeyNoLongerInCacheOrExpired,
                                                       CacheConstants.TimingIterations,
                                                       CacheConstants.TimingIterationInterval,
                                                       CacheConstants.VeryLongDuration);

        using var tempFile2 = new TempFile();
        _ = cache.TrySaveToFile(tempFile2.Info);

        Assert.IsTrue(cleaned);
        Assert.AreEqual(tempFile1.Info.Length, tempFile2.Info.Length);
    }
}
