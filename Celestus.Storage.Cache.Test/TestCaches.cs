using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestCaches
{
    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadCache))]
    public void VerifyThatStaticMethodForSerializationToAndFromFileWorksType(Type cacheType)
    {
        //
        // Arrange
        //
        using var cache = CacheHelper.Create(cacheType, string.Empty);

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
        var saved = cache.TrySaveToFile(tempFile.Uri);

        using var otherCache = CacheHelper.TryCreateFromFile(cacheType, tempFile.Uri);

        //
        // Assert
        //
        Assert.IsTrue(saved);

        Assert.IsNotNull(otherCache);
        Assert.AreEqual(cache, otherCache);

        Assert.AreEqual((true, VALUE_1), otherCache.TryGet<int>(KEY_1));
        Assert.AreEqual((true, VALUE_2), otherCache.TryGet<double>(KEY_2));
        Assert.AreEqual((true, VALUE_3), otherCache.TryGet<DateTime>(KEY_3));
        Assert.AreEqual((true, VALUE_4), otherCache.TryGet<ExampleRecord>(KEY_4));
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadCache))]
    public void VerifyThatSharedCacheCanBeUpdatedFromFile(Type cacheType)
    {
        //
        // Arrange
        //
        using var cache = CacheHelper.GetOrCreateShared(cacheType, nameof(VerifyThatSharedCacheCanBeUpdatedFromFile));

        const string KEY_1 = "Kola";
        const int VALUE_1 = 891237;
        cache.Set(KEY_1, VALUE_1);

        //
        // Act
        //
        using var tempFile = new TempFile();
        _ = cache.TrySaveToFile(tempFile.Uri);

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
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadCache))]
    public void VerifyThatReadingEmptyFileFails(Type cacheType)
    {
        //
        // Arrange
        //
        using var cache = CacheHelper.Create(cacheType, string.Empty);

        using var tempFile = new TempFile();

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
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadCache))]
    public void VerifyThatReadingValueUsingWrongTypeFails(Type cacheType)
    {
        //
        // Arrange
        //
        using var cache = CacheHelper.Create(cacheType, string.Empty);

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

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadCache))]
    public void VerifyThatGetThrowsInvalidOperationExceptionWhenKeyNotFound(Type cacheType)
    {
        //
        // Arrange
        //
        using var cache = CacheHelper.Create(cacheType, string.Empty);

        //
        // Act & Assert
        //
        Assert.ThrowsException<InvalidOperationException>(() => cache.Get<string>("nonexistent"));
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadCache))]
    public void VerifyThatGetThrowsInvalidOperationExceptionWhenWrongType(Type cacheType)
    {
        //
        // Arrange
        //
        using var cache = CacheHelper.Create(cacheType, string.Empty);
        cache.Set("key", 42);

        //
        // Act & Assert
        //
        Assert.ThrowsException<InvalidOperationException>(() => cache.Get<string>("key"));
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadCache))]
    public void VerifyThatNullableTypesWorkCorrectly(Type cacheType)
    {
        //
        // Arrange
        //
        using var cache = CacheHelper.Create(cacheType, string.Empty);

        //
        // Act
        //
        cache.Set<int?>("nullable-int", null);
        cache.Set<string?>("nullable-string", null);
        cache.Set<object?>("nullable-object", null);

        //
        // Assert
        //
        var (intResult, intData) = cache.TryGet<int?>("nullable-int");
        var (stringResult, stringData) = cache.TryGet<string>("nullable-string");
        var (objectResult, objectData) = cache.TryGet<object>("nullable-object");

        Assert.IsTrue(intResult);
        Assert.IsNull(intData);

        Assert.IsTrue(stringResult);
        Assert.IsNull(stringData);

        Assert.IsTrue(objectResult);
        Assert.IsNull(objectData);
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadCache))]
    public void VerifyThatValueTypesHandledCorrectly(Type cacheType)
    {
        //
        // Arrange
        //
        using var cache = CacheHelper.Create(cacheType, string.Empty);

        //
        // Act
        //
        cache.Set("bool", true);
        cache.Set("byte", (byte)255);
        cache.Set("char", 'A');
        cache.Set("decimal", 123.456m);
        cache.Set("double", 123.456d);
        cache.Set("float", 123.456f);
        cache.Set("int", 123);
        cache.Set("long", 123L);
        cache.Set("sbyte", (sbyte)-123);
        cache.Set("short", (short)123);
        cache.Set("uint", (uint)123);
        cache.Set("ulong", (ulong)123);
        cache.Set("ushort", (ushort)123);

        //
        // Assert
        //
        Assert.AreEqual((true, true), cache.TryGet<bool>("bool"));
        Assert.AreEqual((true, (byte)255), cache.TryGet<byte>("byte"));
        Assert.AreEqual((true, 'A'), cache.TryGet<char>("char"));
        Assert.AreEqual((true, 123.456m), cache.TryGet<decimal>("decimal"));
        Assert.AreEqual((true, 123.456d), cache.TryGet<double>("double"));
        Assert.AreEqual((true, 123.456f), cache.TryGet<float>("float"));
        Assert.AreEqual((true, 123), cache.TryGet<int>("int"));
        Assert.AreEqual((true, 123L), cache.TryGet<long>("long"));
        Assert.AreEqual((true, (sbyte)-123), cache.TryGet<sbyte>("sbyte"));
        Assert.AreEqual((true, (short)123), cache.TryGet<short>("short"));
        Assert.AreEqual((true, (uint)123), cache.TryGet<uint>("uint"));
        Assert.AreEqual((true, (ulong)123), cache.TryGet<ulong>("ulong"));
        Assert.AreEqual((true, (ushort)123), cache.TryGet<ushort>("ushort"));
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadCache))]
    public void VerifyThatComplexGenericTypesWork(Type cacheType)
    {
        //
        // Arrange
        //
        using var cache = CacheHelper.Create(cacheType, string.Empty);

        //
        // Act
        //
        var list = new List<string> { "a", "b", "c" };
        var dict = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } };
        var tuple = (42, "test", true);

        cache.Set("list", list);
        cache.Set("dict", dict);
        cache.Set("tuple", tuple);

        //
        // Assert
        //
        var (listResult, listData) = cache.TryGet<List<string>>("list");
        var (dictResult, dictData) = cache.TryGet<Dictionary<string, int>>("dict");
        var (tupleResult, tupleData) = cache.TryGet<(int, string, bool)>("tuple");

        Assert.IsTrue(listResult);
        Assert.IsTrue(dictResult);
        Assert.IsTrue(tupleResult);

        CollectionAssert.AreEqual(list, listData);
        CollectionAssert.AreEqual(dict, dictData);
        Assert.AreEqual(tuple, tupleData);
    }

    [TestMethod]
    [DataRow(typeof(Cache))]
    [DataRow(typeof(ThreadCache))]
    public void VerifyThatEnumTypesWork(Type cacheType)
    {
        //
        // Arrange
        //
        using var cache = CacheHelper.Create(cacheType, string.Empty);

        //
        // Act
        //
        cache.Set("enum", DayOfWeek.Monday);

        //
        // Assert
        //
        var (result, data) = cache.TryGet<DayOfWeek>("enum");
        Assert.IsTrue(result);
        Assert.AreEqual(DayOfWeek.Monday, data);
    }
}
