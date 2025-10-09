using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize]
    public sealed class TestThreadSafeCacheSerialization
    {
        [TestMethod]
        public void VerifyThatSerializationToAndFromFileWorks()
        {
            string cacheKey = nameof(VerifyThatSerializationToAndFromFileWorks);
            using var originalCache = new ThreadSafeCache(cacheKey);
            const string KEY_1 = "serial"; const int VALUE_1 = 57; _ = originalCache.TrySet(KEY_1, VALUE_1);
            const string KEY_2 = "Mattis"; const double VALUE_2 = 11.5679; _ = originalCache.TrySet(KEY_2, VALUE_2);
            const string KEY_3 = "Lakris"; DateTime VALUE_3 = DateTime.Now; _ = originalCache.TrySet(KEY_3, VALUE_3);
            const string KEY_4 = "Ludde"; ExampleRecord VALUE_4 = new(-9634, "VerifyThatSerializationWorks", 10000000M); _ = originalCache.TrySet(KEY_4, VALUE_4);
            using var tempFile = new TempFile();
            var loaded = originalCache.TrySaveToFile(tempFile.Info);
            using ThreadSafeCache? loadedCache = ThreadSafeCache.TryCreateFromFile(tempFile.Info);
            Assert.IsTrue(loaded);
            Assert.IsNotNull(loadedCache);
            Assert.AreEqual(cacheKey, loadedCache.Id);
            Assert.AreEqual(originalCache, loadedCache);
            Assert.AreEqual(originalCache.GetHashCode(), loadedCache.GetHashCode());
            Assert.IsTrue(loadedCache.TryGet<int>(KEY_1, out var value1)); Assert.AreEqual(VALUE_1, value1);
            Assert.IsTrue(loadedCache.TryGet<double>(KEY_2, out var value2)); Assert.AreEqual(VALUE_2, value2);
            Assert.IsTrue(loadedCache.TryGet<DateTime>(KEY_3, out var value3)); Assert.AreEqual(VALUE_3, value3);
            Assert.IsTrue(loadedCache.TryGet<ExampleRecord>(KEY_4, out var value4)); Assert.AreEqual(VALUE_4, value4);
        }

        [TestMethod]
        public void VerifyThatCacheIsNotLoadedWhenKeysDiffer()
        {
            using ThreadSafeCache cacheOne = new("SomeKey");
            const string KEY_1 = "Janta"; const Decimal VALUE_1 = 598888899663145M; _ = cacheOne.TrySet(KEY_1, VALUE_1);
            using var tempFile = new TempFile();
            cacheOne.TrySaveToFile(tempFile.Info);
            using ThreadSafeCache cacheTwo = new("anotherKey");
            var loaded = cacheTwo.TryLoadFromFile(tempFile.Info);
            Assert.IsFalse(loaded);
        }

        [TestMethod]
        public void VerifyThatCacheCanBeUpdatedFromFile()
        {
            using var cache = new ThreadSafeCache();
            const string KEY_1 = "Katter"; const int VALUE_1 = 123; _ = cache.TrySet(KEY_1, VALUE_1);
            using var file = new TempFile();
            _ = cache.TrySaveToFile(file.Info);
            _ = cache.TrySet(KEY_1, VALUE_1 * 2);
            const string KEY_2 = "Explain"; const double VALUE_2 = 78.1234; _ = cache.TrySet(KEY_2, VALUE_2);
            bool loaded = cache.TryLoadFromFile(file.Info);
            Assert.IsTrue(loaded);
            Assert.IsTrue(cache.TryGet<int>(KEY_1, out var value)); Assert.AreEqual(VALUE_1, value);
            Assert.IsFalse(cache.TryGet<double>(KEY_2, out _));
        }

        [TestMethod]
        public void VerifyThatReadingEmptyFileFails()
        {
            using var cache = new ThreadSafeCache();
            using var file = new TempFile();
            File.WriteAllText(file.Info.FullName, "");
            bool loaded = cache.TryLoadFromFile(file.Info);
            Assert.IsFalse(loaded);
        }
    }
}
