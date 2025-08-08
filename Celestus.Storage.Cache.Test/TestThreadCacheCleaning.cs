using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize] // Timing tests become unreliable when run in parallel.
public class TestThreadCacheCleaning
{
    [TestMethod]
    public void VerifyThatCleanerIsInformedOfNewEntries()
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
        const int VALUE_1 = 123456;
        _ = cache.TrySet(KEY_1, VALUE_1);

        const string KEY_2 = "Lion";
        const double VALUE_2 = 1.2567;
        _ = cache.TrySet(KEY_2, VALUE_2);

        //
        // Assert
        //
        Assert.AreEqual(2, cleanerTester.TrackedKeys.Count);
        Assert.AreEqual(KEY_1, cleanerTester.TrackedKeys[0]);
        Assert.AreEqual(KEY_2, cleanerTester.TrackedKeys[1]);
    }

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
        Assert.AreEqual(1, cleanerTester.AccessedKeys.Count);
        Assert.AreEqual(KEY_1, cleanerTester.AccessedKeys[0]);
    }

    [TestMethod]
    public void VerifyThatCleanerIsGivenRemovalCallback()
    {
        //
        // Arrange
        //
        using var cleanerTester = new CacheCleanerTester();
        using var cache = new ThreadCache(cleanerTester);

        const string KEY_1 = "Hamster";
        const bool VALUE_1 = true;
        const int INSTANT_TIMEOUT_IN_MS = 0;
        _ = cache.TrySet(KEY_1, VALUE_1, TimeSpan.FromMilliseconds(INSTANT_TIMEOUT_IN_MS));

        //
        // Act
        //
        if (cleanerTester.RemovalCallback.TryGetTarget(out var callback))
        {
            callback([KEY_1]);
        }

        //
        // Assert
        //
        Assert.AreEqual((false, default), cache.TryGet<bool>(KEY_1));
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
        var path = new Uri(Path.GetTempFileName());
        cache.TrySaveToFile(path);

        using ThreadCache? loadedCache = ThreadCache.TryCreateFromFile(path); ;

        //
        // Assert
        //
        Assert.IsTrue(cleanerTester.SettingsWritten);
        Assert.IsTrue(cleanerTester.SettingsReadCorrectly);

        // Cleanup
        File.Delete(path.AbsolutePath);
    }

    [TestMethod]
    public void VerifyThatCacheFreesUpMemory()
    {
        //
        // Arrange
        //
        const int CLEAN_INTERVAL_IN_MS = 5;
        using var cache = new ThreadCache(new ThreadCacheCleaner<string>(cleanupIntervalInMs: CLEAN_INTERVAL_IN_MS));

        static byte[] CreateElement()
        {
            const int ELEMENT_SIZE = 10;
            return new byte[ELEMENT_SIZE];
        }

        var keys = new SerialKeys();
        var firstKey = keys.Next();
        _ = cache.TrySet(firstKey, CreateElement(), TimeSpan.FromDays(1));

        var path_1 = new Uri(Path.GetTempFileName());
        cache.TrySaveToFile(path_1);

        //
        // Act
        //
        const int N_ITERATIONS = 1000;

        for (int i = 0; i < N_ITERATIONS; i++)
        {
            _ = cache.TrySet(keys.Next(), CreateElement(), TimeSpan.FromMilliseconds(CLEAN_INTERVAL_IN_MS));
        }

        ThreadHelper.SpinWait(ThreadCacheConstants.LONG_INTERVAL_IN_MS);

        _ = cache.TryGet<byte[]>(firstKey);

        var path_2 = new Uri(Path.GetTempFileName());
        cache.TrySaveToFile(path_2);

        //
        // Assert
        //
        var file_1 = new FileInfo(path_1.AbsolutePath);
        var file_2 = new FileInfo(path_2.AbsolutePath);

        Assert.AreEqual(file_1.Length, file_2.Length);

        // Cleanup
        file_1.Delete();
        file_2.Delete();
    }
}
