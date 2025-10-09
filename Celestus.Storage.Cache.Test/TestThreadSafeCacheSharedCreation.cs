using Celestus.Io;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize]
    public sealed class TestThreadSafeCacheSharedCreation
    {
        [TestMethod]
        public void VerifyThatThreadSafeCacheLoadingFromNonExistentFileReturnsNull()
        {
            var nonExistentFile = new FileInfo("C:/Users/so/non-existent-file.json");
            var cache = ThreadSafeCache.TryCreateFromFile(nonExistentFile);
            Assert.IsNull(cache);
        }

        [TestMethod]
        public void VerifyThatSharedCachesWithDifferentKeysAreUnique()
        {
            using var cache = ThreadSafeCache.Factory.GetOrCreateShared(nameof(VerifyThatSharedCachesWithDifferentKeysAreUnique));
            using var otherCache = ThreadSafeCache.Factory.GetOrCreateShared(new Guid().ToString());
            const int VALUE = 55; const string KEY = "test"; _ = cache.TrySet(KEY, VALUE);
            _ = otherCache.TrySet(KEY, VALUE + 1); _ = otherCache.TryGet<int>(KEY, out var otherValue);
            Assert.IsTrue(cache.TryGet<int>(KEY, out var value1)); Assert.AreEqual(VALUE, value1);
            Assert.AreEqual(VALUE + 1, otherValue);
        }

        [TestMethod]
        public void VerifyThatSharedCacheFailsWhenLoadedFromCorruptFile()
        {
            using var file = new TempFile();
            File.WriteAllText(file.Info.FullName, "");
            using var cache = ThreadSafeCache.TryCreateFromFile(file.Info);
            Assert.IsNull(cache);
        }

        [TestMethod]
        public void VerifyThatSharedCacheCanBeRetrievedAfterCreated()
        {
            const string ELEMENT_KEY = "kii"; const string ELEMENT_VALUE = "ads4s65ad4a6s8d4a8sd478asd4asd8546asd56"; const string CACHE_KEY = nameof(VerifyThatSharedCacheCanBeRetrievedAfterCreated);
            using var cache = ThreadSafeCache.Factory.GetOrCreateShared(CACHE_KEY);
            _ = cache.TrySet(ELEMENT_KEY, ELEMENT_VALUE);
            var otherCache = ThreadSafeCache.Factory.GetOrCreateShared(CACHE_KEY);
            Assert.IsTrue(otherCache.TryGet<string>(ELEMENT_KEY, out var element)); Assert.AreEqual(ELEMENT_VALUE, element);
            Assert.AreSame(cache, otherCache);
        }

        [TestMethod]
        public void VerifyThatSharedCacheCanBeLoadedFromFileWhenKeyIsNotKnown()
        {
            const string ELEMENT_KEY = "kii"; const string ELEMENT_VALUE = "ads4s65ad4a6s8d4a8sd478asd4asd8546asd56"; const string CACHE_KEY = nameof(VerifyThatSharedCacheCanBeLoadedFromFileWhenKeyIsNotKnown);
            using var cache = new ThreadSafeCache(CACHE_KEY);
            _ = cache.TrySet(ELEMENT_KEY, ELEMENT_VALUE);
            using var tempFile = new TempFile();
            _ = cache.TrySaveToFile(tempFile.Info);
            using ThreadSafeCache? otherCache = ThreadSafeCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Info);
            Assert.IsNotNull(otherCache);
            Assert.IsTrue(otherCache.TryGet<string>(ELEMENT_KEY, out var element)); Assert.AreEqual(ELEMENT_VALUE, element);
        }

        [TestMethod]
        public void VerifyThatSharedCacheCanBeUpdatedFromFile()
        {
            using ThreadSafeCache cache = ThreadSafeCache.Factory.GetOrCreateShared(nameof(VerifyThatSharedCacheCanBeUpdatedFromFile));
            const string KEY_1 = "Katter"; const int VALUE_1 = 123; _ = cache.TrySet(KEY_1, VALUE_1);
            using var tempFile = new TempFile();
            _ = cache.TrySaveToFile(tempFile.Info);
            _ = cache.TrySet(KEY_1, VALUE_1 * 2);
            const string KEY_2 = "Cat"; const double VALUE_2 = 78.1234; _ = cache.TrySet(KEY_2, VALUE_2);
            ThreadSafeCache? otherCache = ThreadSafeCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Info);
            Assert.IsNotNull(otherCache); Assert.AreEqual(cache, otherCache);
            Assert.IsTrue(cache.TryGet<int>(KEY_1, out var updated)); Assert.AreEqual(VALUE_1, updated);
            Assert.IsFalse(cache.TryGet<double>(KEY_2, out _));
        }

        [TestMethod]
        public void VerifyThatReadingSharedEmptyFileFails()
        {
            using var tempFile = new TempFile();
            using ThreadSafeCache? cache = ThreadSafeCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Info);
            Assert.IsNull(cache);
        }
    }
}
