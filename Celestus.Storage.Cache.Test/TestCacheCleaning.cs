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
        var cleanerTester = new CacheCleanerTester();
        var cache = new Cache(cleanerTester);

        //
        // Act
        //
        var path = new Uri(Path.GetTempFileName());
        _ = cache.TrySaveToFile(path);
        _ = Cache.TryCreateFromFile(path);

        File.Delete(path.AbsolutePath);

        //
        // Assert
        //
        Assert.IsTrue(cleanerTester.SettingsWritten);
        Assert.IsTrue(cleanerTester.SettingsReadCorrectly);
    }

    [TestMethod]
    [DoNotParallelize]
    public void VerifyThatCacheFreesUpMemory()
    {
        //
        // Arrange
        //
        GC.Collect();
        GC.WaitForPendingFinalizers();

        using var cache = new Cache(new CacheCleaner<string>(interval: CacheConstants.ShortDuration));

        static byte[] CreateElement()
        {
            const int ELEMENT_SIZE = 10;
            return new byte[ELEMENT_SIZE];
        }

        var keys = new SerialKeys();
        var firstKey = keys.Next();
        cache.Set(firstKey, CreateElement(), TimeSpan.FromDays(1));

        using var tempFileA = new TempFile();

        _ = cache.TrySaveToFile(tempFileA.Uri);

        //
        // Act
        //
        const int N_ITERATIONS = 1000;

        for (int i = 0; i < N_ITERATIONS; i++)
        {
            cache.Set(keys.Next(), CreateElement(), CacheConstants.ShortDuration);
        }

        ThreadHelper.SpinWait(CacheConstants.TimingDuration);

        var result = cache.TryGet<byte[]>(firstKey);

        using var tempFileB = new TempFile();
        _ = cache.TrySaveToFile(tempFileB.Uri);

        var removeLast = cache.TryRemove([firstKey]);

        for (int i = 0; i < N_ITERATIONS; i++)
        {
            cache.Set(keys.Next(), CreateElement(), CacheConstants.ShortDuration);
        }

        //
        // Assert
        //
        var fileA = tempFileA.ToFileInfo();
        var fileB = tempFileB.ToFileInfo();

        Assert.IsTrue(removeLast);
        Assert.IsTrue(result is (true, _));
        Assert.AreEqual(fileA.Length, fileB.Length);
    }

    [TestMethod]
    public void VerifyThatMissingIntervalCausesCrash()
    {
        //
        // Arrange
        //
        string json = "{\"ExtraParameter\":\"500\"}";
        Assert.ThrowsException<MissingValueJsonException>(() => CleaningHelper.ReadSettings<CacheCleaner<string>>(json));
    }

    [TestMethod]
    public void VerifyThatUnknownParametersAreIgnored()
    {
        string json = "{\"ExtraParameter\":\"500\",\"_cleanupIntervalInTicks\":500}";
        CleaningHelper.ReadSettings<CacheCleaner<string>>(json);
    }
}
