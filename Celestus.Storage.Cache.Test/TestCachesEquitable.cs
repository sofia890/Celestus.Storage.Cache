using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestCachesEquitable
{
    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadSafeCache))]
    public void VerifyThatCacheWhenComparedToNullReturnsFalse(Type cacheType)
    {
        //
        // Arrange & Act
        //
        using ICacheBase<string, string> cache = CacheHelper.Create(cacheType, string.Empty);

        //
        // Assert
        //
        Assert.IsFalse(cache.Equals(null));
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadSafeCache))]
    public void VerifyThatCachesWithDifferentKeysAreNotEqual(Type cacheType)
    {
        //
        // Arrange & Act
        //
        const string ID_A = "A";
        const string ID_B = "B";
        using ICacheBase<string, string> cacheA = CacheHelper.Create(cacheType, ID_A);
        using ICacheBase<string, string> cacheB = CacheHelper.Create(cacheType, ID_B);

        //
        // Assert
        //
        Assert.AreNotEqual(cacheA, cacheB);
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadSafeCache))]
    public void VerifyThatCachesWithSameIdAreEqual(Type cacheType)
    {
        //
        // Arrange & Act
        //
        const string ID = "A";
        using ICacheBase<string, string> cacheA = CacheHelper.Create(cacheType, ID);
        using ICacheBase<string, string> cacheB = CacheHelper.Create(cacheType, ID);

        //
        // Assert
        //
        Assert.AreEqual(cacheA, cacheB);
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadSafeCache))]
    public void VerifyThatCachesWithSameIdButDifferentPersistenceAreNotEqual(Type cacheType)
    {
        //
        // Arrange & Act
        //
        const string ID = "A";
        using var tempFileA = new TempFile();
        using ICacheBase<string, string> cacheA = CacheHelper.Create(cacheType, ID, true, tempFileA.Info);
        using ICacheBase<string, string> cacheB = CacheHelper.Create(cacheType, ID, false);

        //
        // Assert
        //
        Assert.AreNotEqual(cacheA, cacheB);
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadSafeCache))]
    public void VerifyThatCachesWithSameIdButDifferentPersistencePathsAreNotEqual(Type cacheType)
    {
        //
        // Arrange & Act
        //
        const string ID = "A";
        using var tempFileA = new TempFile();
        using ICacheBase<string, string> cacheA = CacheHelper.Create(cacheType, ID, true, tempFileA.Info);

        using var tempFileB = new TempFile();
        using ICacheBase<string, string> cacheB = CacheHelper.Create(cacheType, ID, true, tempFileB.Info);

        //
        // Assert
        //
        Assert.AreNotEqual(cacheA, cacheB);
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadSafeCache))]
    public void VerifyThatCachesWithSameIdAndEntriesAreEqual(Type cacheType)
    {
        //
        // Arrange
        //
        const string ID_A = "A";
        using ICacheBase<string, string> cacheA = CacheHelper.Create(cacheType, ID_A);

        const string ID_B = "A";
        using ICacheBase<string, string> cacheB = CacheHelper.Create(cacheType, ID_B);

        //
        // Act
        //
        const string KEY = "key";
        const string VALUE = "value";
        cacheA.Set(KEY, VALUE);
        cacheB.Set(KEY, VALUE);

        //
        // Assert
        //
        Assert.AreEqual(cacheA, cacheB);
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadSafeCache))]
    public void VerifyThatCachesWithSameIdAndKeysButDifferentValuesAreNotEqual(Type cacheType)
    {
        //
        // Arrange
        //
        const string ID_A = "A";
        using ICacheBase<string, string> cacheA = CacheHelper.Create(cacheType, ID_A);

        const string ID_B = "A";
        using ICacheBase<string, string> cacheB = CacheHelper.Create(cacheType, ID_B);

        //
        // Act
        //
        const string KEY = "key";
        const string VALUE_A = "value";
        cacheA.Set(KEY, VALUE_A);

        const string VALUE_B = "4";
        cacheB.Set(KEY, VALUE_B);

        //
        // Assert
        //
        Assert.AreNotEqual(cacheA, cacheB);
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadSafeCache))]
    public void VerifyThatCachesWithSameIdButDifferentKeysWithSameValueAreNotEqual(Type cacheType)
    {
        //
        // Arrange
        //
        const string ID_A = "A";
        using ICacheBase<string, string> cacheA = CacheHelper.Create(cacheType, ID_A);

        const string ID_B = "A";
        using ICacheBase<string, string> cacheB = CacheHelper.Create(cacheType, ID_B);

        //
        // Act
        //
        const string KEY_A = "keyA";
        const string VALUE_A = "value";
        cacheA.Set(KEY_A, VALUE_A);

        const string KEY_B = "keyB";
        const string VALUE_B = "value";
        cacheB.Set(KEY_B, VALUE_B);

        //
        // Assert
        //
        Assert.AreNotEqual(cacheA, cacheB);
    }
}
