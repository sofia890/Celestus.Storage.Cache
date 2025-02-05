using Celestus.Storage.Cache.Test.Model;
using System.Text.Json;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestCacheCleaning
{
    [TestMethod]
    public void VerifyThatCleanerIsInformedOfNewEntries()
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
        const int VALUE_1 = 123456;
        cache.Set(KEY_1, VALUE_1);

        const string KEY_2 = "Lion";
        const double VALUE_2 = 1.2567;
        cache.Set(KEY_2, VALUE_2);

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
        Assert.AreEqual(1, cleanerTester.AccessedKeys.Count);
        Assert.AreEqual(KEY_1, cleanerTester.AccessedKeys[0]);
    }

    [TestMethod]
    public void VerifyThatCleanerIsGivenRemovalCallback()
    {
        //
        // Arrange
        //
        var cleanerTester = new CacheCleanerTester();
        var cache = new Cache(cleanerTester);

        const string KEY_1 = "Hamster";
        const bool VALUE_1 = true;
        const int INSTANT_TIMEOUT = 0;
        cache.Set(KEY_1, VALUE_1, INSTANT_TIMEOUT);

        //
        // Act
        //
        cleanerTester.RemovalCallback([KEY_1]);

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
        var cleanerTester = new CacheCleanerTester();
        var cache = new Cache(cleanerTester);

        //
        // Act
        //
        var path = new Uri(Path.GetTempFileName());
        cache.SaveToFile(path);

        var _ = Cache.TryCreateFromFile(path);

        File.Delete(path.AbsolutePath);

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
        const int CLEAN_INTERVAL_IN_MS = 1;
        var cache = new Cache(new CacheCleaner<string>(cleanupIntervalInMs: CLEAN_INTERVAL_IN_MS), false);

        static byte[] CreateElement()
        {
            const int ELEMENT_SIZE = 10;
            return new byte[ELEMENT_SIZE];
        }

        var keys = new SerialKeys();
        var firstKey = keys.Next();
        cache.Set(firstKey, CreateElement(), TimeSpan.FromDays(1));

        var path_1 = new Uri(Path.GetTempFileName());
        cache.SaveToFile(path_1);

        //
        // Act
        //
        const int N_ITERATIONS = 1000;

        for (int i = 0; i < N_ITERATIONS; i++)
        {
            cache.Set(keys.Next(), CreateElement(), TimeSpan.FromMilliseconds(CLEAN_INTERVAL_IN_MS));
        }

        ThreadHelper.SpinWait(CLEAN_INTERVAL_IN_MS);

        _ = cache.TryGet<byte[]>(firstKey);

        var path_2 = new Uri(Path.GetTempFileName());
        cache.SaveToFile(path_2);

        //
        // Assert
        //
        var file_1 = new FileInfo(path_1.AbsolutePath);
        var file_2 = new FileInfo(path_2.AbsolutePath);

        Assert.AreEqual(file_1.Length, file_2.Length);

        file_1.Delete();
        file_2.Delete();
    }


    [TestMethod]
    public void VerifyThatMissingIntervalCausesCrash()
    {
        //
        // Arrange
        //
        string json = "{\"ExtraParameter\":\"500\"}";
        Assert.ThrowsException<JsonException>(() => CleaningHelper.ReadSettings<CacheCleaner<string>>(json));
    }

    [TestMethod]
    public void VerifyThatUnknownParametersAreIgnored()
    {
        string json = "{\"ExtraParameter\":\"500\",\"_cleanupIntervalInTicks\":500}";
        CleaningHelper.ReadSettings<CacheCleaner<string>>(json);
    }
}
