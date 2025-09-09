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

        _ = cache.TryGet<bool>(KEY_1);

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

        static byte[] CreateElement()
        {
            const int ELEMENT_SIZE = 10;
            return new byte[ELEMENT_SIZE];
        }

        var keys = new SerialKeys();
        var firstKey = keys.Next();
        _ = cache.TrySet(firstKey, CreateElement(), TimeSpan.FromDays(1));

        using var tempFile1 = new TempFile();
        _ = cache.TrySaveToFile(tempFile1.Uri);

        //
        // Act
        //
        const int N_ITERATIONS = 1000;

        for (int i = 0; i < N_ITERATIONS; i++)
        {
            _ = cache.TrySet(keys.Next(), CreateElement(), CacheConstants.ShortDuration);
        }

        ThreadHelper.SpinWait(interval);

        ThreadHelper.DoPeriodicallyUntil(() => cache.TryGet<byte[]>(firstKey, CacheConstants.ShortDuration) is (false, _),
                                         CacheConstants.TimingIterations,
                                         CacheConstants.TimingIterationInterval,
                                         CacheConstants.VeryLongDuration);

        using var tempFile2 = new TempFile();
        _ = cache.TrySaveToFile(tempFile2.Uri);

        //
        // Assert
        //
        var file_1 = tempFile1.ToFileInfo();
        var file_2 = tempFile2.ToFileInfo();

        Assert.AreEqual(file_1.Length, file_2.Length);
    }
}
