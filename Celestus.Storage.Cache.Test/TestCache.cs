using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestCache
{
    [TestMethod]
    public void VerifyThatSerializationToAndFromFileWorks()
    {
        //
        // Arrange
        //
        using Cache cache = new();

        const string KEY_1 = "One";
        const int VALUE_1 = 757;
        cache.Set(KEY_1, VALUE_1);

        const string KEY_2 = "Two";
        const double VALUE_2 = 11000.579;
        cache.Set(KEY_2, VALUE_2);

        const string KEY_3 = "Three";
        DateTime VALUE_3 = DateTime.Today;
        cache.Set(KEY_3, VALUE_3);

        const string KEY_4 = "Ludde";
        ExampleRecord VALUE_4 = new(-9634, "VerifyThatSerializationWorks", 10000000M);
        cache.Set(KEY_4, VALUE_4);

        //
        // Act
        //
        using var tempFile = new TempFile();
        cache.SaveToFile(tempFile.Uri);

        using Cache? otherCache = Cache.TryCreateFromFile(tempFile.Uri);

        //
        // Assert
        //
        Assert.IsNotNull(otherCache);
        Assert.AreEqual(cache, otherCache);

        Assert.AreEqual((true, VALUE_1), otherCache.TryGet<int>(KEY_1));
        Assert.AreEqual((true, VALUE_2), otherCache.TryGet<double>(KEY_2));
        Assert.AreEqual((true, VALUE_3), otherCache.TryGet<DateTime>(KEY_3));
        Assert.AreEqual((true, VALUE_4), otherCache.TryGet<ExampleRecord>(KEY_4));
    }

    [TestMethod]
    public void VerifyThatSharedCacheCanBeUpdatedFromFile()
    {
        //
        // Arrange
        //
        using Cache cache = new();

        const string KEY_1 = "Kola";
        const int VALUE_1 = 891237;
        cache.Set(KEY_1, VALUE_1);

        //
        // Act
        //
        using var tempFile = new TempFile();
        cache.SaveToFile(tempFile.Uri);

        cache.Set(KEY_1, VALUE_1 * 2);

        const string KEY_2 = "Lakris";
        const double VALUE_2 = 988.1234;
        cache.Set(KEY_2, VALUE_2);

        cache.TryLoadFromFile(tempFile.Uri);

        //
        // Assert
        //
        Assert.AreEqual((true, VALUE_1), cache.TryGet<int>(KEY_1));
        Assert.AreEqual((false, 0), cache.TryGet<double>(KEY_2));
    }

    [TestMethod]
    public void VerifyThatReadingEmptyFileFails()
    {
        //
        // Arrange
        //
        using Cache cache = new();

        using var tempFile = new TempFile(createFile: true);

        //
        // Act
        //
        bool loaded = cache.TryLoadFromFile(tempFile.Uri);

        //
        // Assert
        //
        Assert.IsFalse(loaded);
    }

    [TestMethod]
    public void VerifyThatReadingValueUsingWrongTypeFails()
    {
        //
        // Arrange
        //
        using Cache cache = new();

        const string KEY_1 = "Kola";
        const int VALUE_1 = 891237;
        cache.Set(KEY_1, VALUE_1);

        //
        // Act
        //
        var asString = cache.TryGet<string>(KEY_1);
        var asDouble = cache.TryGet<double>(KEY_1);
        var asDateTime = cache.TryGet<DateTime>(KEY_1);

        //
        // Assert
        //
        Assert.AreEqual((false, default), asString);
        Assert.AreEqual((false, default), asDouble);
        Assert.AreEqual((false, default), asDateTime);
    }
}
