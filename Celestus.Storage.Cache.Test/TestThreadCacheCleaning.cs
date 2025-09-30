using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize] // Timing tests become unreliable when run in parallel.
public class TestThreadCacheCleaning
{
    [TestMethod]
    public void VerifyThatCleanerIsInformedWhenEntriesAreAccessed()
    {
        //
        // Arrange
        //
        using var cleanerTester = new CacheCleanerTester();
        using var cache = new ThreadCache(cleanerTester);

        //
        // Act
        //
        const string KEY_1 = "Hamster";
        const bool VALUE_1 = true;
        _ = cache.TrySet(KEY_1, VALUE_1);

        _ = cache.TryGet<bool>(KEY_1, out _);

        //
        // Assert
        //
        Assert.AreEqual(2, cleanerTester.AccessedKeys.Count);
        Assert.AreEqual(KEY_1, cleanerTester.AccessedKeys[0]);
    }

    [TestMethod]
    public void VerifyThatCleanerSerializationIsConnected()
    {
        //
        // Arrange
        //
        using var cleanerTester = new CacheCleanerTester();
        using var cache = new ThreadCache(cleanerTester);

        //
        // Act
        //
        using var tempFile = new TempFile();
        _ = cache.TrySaveToFile(tempFile.Uri);

        using ThreadCache? loadedCache = ThreadCache.TryCreateFromFile(tempFile.Uri);

        //
        // Assert
        //
        Assert.IsTrue(cleanerTester.SettingsWritten);
        Assert.IsTrue(cleanerTester.SettingsReadCorrectly);
    }

    [TestMethod]
    public void VerifyThatCacheFreesUpMemory()
    {
        //
        // Arrange
        //
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var interval = CacheConstants.TimingDuration;
        using var cache = new ThreadCache(interval);

        var keys = new SerialKeys();
        var firstKey = keys.Next();
        _ = cache.TrySet(firstKey, ElementHelper.CreateSmallArray(), TimeSpan.FromDays(1));

        using var tempFile1 = new TempFile();
        _ = cache.TrySaveToFile(tempFile1.Uri);

        //
        // Act
        //
        const int NROF_KEYS = 1000;

        for (int i = 0; i < NROF_KEYS; i++)
        {
            _ = cache.TrySet(keys.Next(), ElementHelper.CreateSmallArray(), CacheConstants.ShortDuration);
        }

        bool LastKeyNoLongerInCache()
        {
            var success = cache.TryGet<byte[]>(keys.Current(), out var value, timeout: CacheConstants.TimingIterationInterval);
            return !success && value == null;
        }
        var cleaned = ThreadHelper.DoPeriodicallyUntil(LastKeyNoLongerInCache,
                                                       CacheConstants.TimingIterations,
                                                       CacheConstants.TimingIterationInterval,
                                                       CacheConstants.VeryLongDuration);

        using var tempFile2 = new TempFile();
        _ = cache.TrySaveToFile(tempFile2.Uri);

        //
        // Assert
        //
        Assert.IsTrue(cleaned);

        var file_1 = tempFile1.ToFileInfo();
        var file_2 = tempFile2.ToFileInfo();

        Assert.AreEqual(file_1.Length, file_2.Length);
    }
}
