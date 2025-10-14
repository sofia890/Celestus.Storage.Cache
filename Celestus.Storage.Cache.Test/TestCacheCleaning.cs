using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestCacheCleaning
{
    [TestMethod]
    public void VerifyThatCleanerIsInformedWhenEntriesAreAccessed()
    {
        //
        // Arrange
        //
        var cleanerTester = new CacheCleanerTester();
        var cache = new Cache(cleanerTester);

        //
        // Act
        //
        const string KEY_1 = "Hamster";
        const bool VALUE_1 = true;
        cache.Set(KEY_1, VALUE_1);

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
        var cleanerTester = new CacheCleanerTester();
        var cache = new Cache(cleanerTester);

        //
        // Act
        //
        using var file = new TempFile();

        _ = cache.TrySaveToFile(file.Info);
        var otherCache = Cache.TryCreateFromFile(file.Info);

        //
        // Assert
        //
        Assert.IsTrue(cleanerTester.SettingsWritten);
        Assert.IsNotNull(otherCache);
        Assert.IsTrue(((CacheCleanerTester)otherCache.Cleaner).SettingsReadCorrectly);
    }

    [TestMethod]
    public void VerifyThatCacheFreesUpMemory()
    {
        //
        // Arrange
        //
        GC.Collect();
        GC.WaitForPendingFinalizers();

        using var cache = new Cache(new CacheCleaner<string, string>(interval: CacheConstants.ShortDuration));

        static byte[] CreateElement()
        {
            const int ELEMENT_SIZE = 10;
            return new byte[ELEMENT_SIZE];
        }

        var keys = new SerialKeys();
        var firstKey = keys.Next();
        cache.Set(firstKey, CreateElement(), TimeSpan.FromDays(1));

        using var tempFileA = new TempFile();

        _ = cache.TrySaveToFile(tempFileA.Info);

        //
        // Act
        //
        const int N_ITERATIONS = 1000;

        for (int i = 0; i < N_ITERATIONS; i++)
        {
            cache.Set(keys.Next(), CreateElement(), CacheConstants.ShortDuration);
        }

        ThreadHelper.SpinWait(CacheConstants.TimingDuration);

        // Triggers cleanup
        var result = cache.TryGet<byte[]>(firstKey, out var firstValue);

        using var tempFileB = new TempFile();
        _ = cache.TrySaveToFile(tempFileB.Info);

        // Verify that the first key did not expire.
        var removeLast = cache.TryRemove([firstKey]);

        //
        // Assert
        //
        var fileA = tempFileA.Info;
        var fileB = tempFileB.Info;

        Assert.IsTrue(removeLast);
        Assert.IsTrue(result);
        Assert.IsNotNull(firstValue);
        Assert.AreEqual(fileA.Length, fileB.Length);
    }

    [TestMethod]
    public void VerifyThatMissingIntervalCausesCrash()
    {
        string json = "{\"ExtraParameter\":\"500\"}";
        Assert.ThrowsException<MissingValueJsonException>(() => CleaningHelper.Deserialize<CacheCleaner<string, string>>(json));
    }

    [TestMethod]
    public void VerifyThatUnknownParametersAreIgnored()
    {
        string json = "{\"ExtraParameter\":\"500\",\"CleanupInterval\":\"00:00:00.5\"}";
        CleaningHelper.Deserialize<CacheCleaner<string, string>>(json);
    }
}
