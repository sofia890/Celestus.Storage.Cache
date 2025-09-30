using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;
using Newtonsoft.Json.Linq;

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

        Assert.IsTrue(otherCache.TryGet<int>(KEY_1, out var value1));
        Assert.AreEqual(VALUE_1, value1);

        Assert.IsTrue(otherCache.TryGet<double>(KEY_2, out var value2));
        Assert.AreEqual(VALUE_2, value2);

        Assert.IsTrue(otherCache.TryGet<DateTime>(KEY_3, out var value3));
        Assert.AreEqual(VALUE_3, value3);

        Assert.IsTrue(otherCache.TryGet<ExampleRecord>(KEY_4, out var value4));
        Assert.AreEqual(VALUE_4, value4);
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
        Assert.IsTrue(cache.TryGet<int>(KEY_1, out var restored));
        Assert.AreEqual(VALUE_1, restored);

        Assert.IsFalse(cache.TryGet<double>(KEY_2, out _));
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
        var stringAttempt = cache.TryGet<string>(KEY_1, out _);
        var doubleAttempt = cache.TryGet<double>(KEY_1, out _);
        var dateTimeAttempt = cache.TryGet<DateTime>(KEY_1, out _);

        //
        // Assert
        //
        Assert.IsFalse(stringAttempt);
        Assert.IsFalse(doubleAttempt);
        Assert.IsFalse(dateTimeAttempt);
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
        Assert.IsTrue(cache.TryGet<int?>("nullable-int", out var intData));
        Assert.IsNull(intData);

        Assert.IsTrue(cache.TryGet<string>("nullable-string", out var stringData));
        Assert.IsNull(stringData);

        Assert.IsTrue(cache.TryGet<object>("nullable-object", out var objectData));
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
        const string KEY_BOOL = "bool";
        const bool VALUE_BOOL = true;
        cache.Set(KEY_BOOL, VALUE_BOOL);

        const string KEY_BYTE = "byte";
        const byte VALUE_BYTE = (byte)255;
        cache.Set(KEY_BYTE, VALUE_BYTE);

        const string KEY_CHAR = "char";
        const char VALUE_CHAR = 'A';
        cache.Set(KEY_CHAR, VALUE_CHAR);

        const string KEY_DECIMAL = "decimal";
        const decimal VALUE_DECIMAL = 123.456m;
        cache.Set(KEY_DECIMAL, VALUE_DECIMAL);

        const string KEY_DOUBLE = "double";
        const double VALUE_DOUBLE = 123.456d;
        cache.Set(KEY_DOUBLE, VALUE_DOUBLE);

        const string KEY_FLOAT = "float";
        const float VALUE_FLOAT = 123.456f;
        cache.Set(KEY_FLOAT, VALUE_FLOAT);

        const string KEY_INT = "int";
        const int VALUE_INT = 123;
        cache.Set(KEY_INT, VALUE_INT);

        const string KEY_LONG = "long";
        const long VALUE_LONG = 123L;
        cache.Set(KEY_LONG, VALUE_LONG);

        const string KEY_SBYTE = "sbyte";
        const sbyte VALUE_SBYTE = (sbyte)-123;
        cache.Set(KEY_SBYTE, VALUE_SBYTE);

        const string KEY_SHORT = "short";
        const short VALUE_SHORT = (short)123;
        cache.Set(KEY_SHORT, VALUE_SHORT);

        const string KEY_UINT = "uint";
        const uint VALUE_UINT = (uint)123;
        cache.Set(KEY_UINT, VALUE_UINT);

        const string KEY_ULONG = "ulong";
        const ulong VALUE_ULONG = (ulong)123;
        cache.Set(KEY_ULONG, VALUE_ULONG);

        const string KEY_USHORT = "ushort";
        const ushort VALUE_USHORT = (ushort)123;
        cache.Set(KEY_USHORT, VALUE_USHORT);

        //
        // Assert
        //
        Assert.IsTrue(cache.TryGet<bool>(KEY_BOOL, out var valueBool));
        Assert.AreEqual(VALUE_BOOL, valueBool);

        Assert.IsTrue(cache.TryGet<byte>(KEY_BYTE, out var valueByte));
        Assert.AreEqual(VALUE_BYTE, valueByte);

        Assert.IsTrue(cache.TryGet<char>(KEY_CHAR, out var valueChar));
        Assert.AreEqual(VALUE_CHAR, valueChar);

        Assert.IsTrue(cache.TryGet<decimal>(KEY_DECIMAL, out var valueDecimal));
        Assert.AreEqual(VALUE_DECIMAL, valueDecimal);

        Assert.IsTrue(cache.TryGet<double>(KEY_DOUBLE, out var valueDouble));
        Assert.AreEqual(VALUE_DOUBLE, valueDouble);

        Assert.IsTrue(cache.TryGet<float>(KEY_FLOAT, out var valueFloat));
        Assert.AreEqual(VALUE_FLOAT, valueFloat);

        Assert.IsTrue(cache.TryGet<int>(KEY_INT, out var valueInt));
        Assert.AreEqual(VALUE_INT, valueInt);

        Assert.IsTrue(cache.TryGet<long>(KEY_LONG, out var valueLong));
        Assert.AreEqual(VALUE_LONG, valueLong);

        Assert.IsTrue(cache.TryGet<sbyte>(KEY_SBYTE, out var valueSByte));
        Assert.AreEqual(VALUE_SBYTE, valueSByte);

        Assert.IsTrue(cache.TryGet<short>(KEY_SHORT, out var valueShort));
        Assert.AreEqual(VALUE_SHORT, valueShort);

        Assert.IsTrue(cache.TryGet<uint>(KEY_UINT, out var valueUInt));
        Assert.AreEqual(VALUE_UINT, valueUInt);

        Assert.IsTrue(cache.TryGet<ulong>(KEY_ULONG, out var valueULong));
        Assert.AreEqual(VALUE_ULONG, valueULong);

        Assert.IsTrue(cache.TryGet<ushort>(KEY_USHORT, out var valueUShort));
        Assert.AreEqual(VALUE_USHORT, valueUShort);
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
        Assert.IsTrue(cache.TryGet<List<string>>("list", out var listData));
        Assert.IsTrue(cache.TryGet<Dictionary<string, int>>("dict", out var dictData));
        Assert.IsTrue(cache.TryGet<(int, string, bool)>("tuple", out var tupleData));

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
        Assert.IsTrue(cache.TryGet<DayOfWeek>("enum", out var data));
        Assert.AreEqual(DayOfWeek.Monday, data);
    }
}
