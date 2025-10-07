using Celestus.Io;
using Newtonsoft.Json.Linq;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not thread safe since they dispose of resource other tests use.
    public sealed class TestThreadCacheSharedCreation
    {
        [TestMethod]
        public void VerifyThatThreadCacheLoadingFromNonExistentFileReturnsNull()
        {
            //
            // Arrange
            //
            var nonExistentFile = new FileInfo("C:/Users/so/non-existent-file.json");

            //
            // Act
            //
            var cache = ThreadCache.TryCreateFromFile(nonExistentFile);

            //
            // Assert
            //
            Assert.IsNull(cache);
        }

        [TestMethod]
        public void VerifyThatSharedCachesWithDifferentKeysAreUnique()
        {
            //
            // Arrange
            //
            using var cache = ThreadCache.Factory.GetOrCreateShared(nameof(VerifyThatSharedCachesWithDifferentKeysAreUnique));
            using var otherCache = ThreadCache.Factory.GetOrCreateShared(new Guid().ToString());
            const int VALUE = 55;
            const string KEY = "test";
            _ = cache.TrySet(KEY, VALUE);

            //
            // Act
            //
            _ = otherCache.TrySet(KEY, VALUE + 1);
            _ = otherCache.TryGet<int>(KEY, out var otherValue);

            //
            // Assert
            //
            Assert.IsTrue(cache.TryGet<int>(KEY, out var value1));
            Assert.AreEqual(VALUE, value1);

            Assert.AreEqual(VALUE + 1, otherValue);
        }

        [TestMethod]
        public void VerifyThatSharedCacheFailsWhenLoadedFromCorruptFile()
        {
            //
            // Arrange
            //
            using var file = new TempFile();

            File.WriteAllText(file.Info.FullName, "");

            //
            // Act
            //

            using var cache = ThreadCache.TryCreateFromFile(file.Info);

            //
            // Assert
            //
            Assert.IsNull(cache);
        }

        [TestMethod]
        public void VerifyThatSharedCacheCanBeRetrievedAfterCreated()
        {
            //
            // Arrange
            //
            const string ELEMENT_KEY = "kii";
            const string ELEMENT_VALUE = "ads4s65ad4a6s8d4a8sd478asd4asd8546asd56";
            const string CACHE_KEY = nameof(VerifyThatSharedCacheCanBeRetrievedAfterCreated);

            using var cache = ThreadCache.Factory.GetOrCreateShared(CACHE_KEY);

            _ = cache.TrySet(ELEMENT_KEY, ELEMENT_VALUE);

            //
            // Act
            //
            var otherCache = ThreadCache.Factory.GetOrCreateShared(CACHE_KEY);

            //
            // Assert
            //
            Assert.IsTrue(otherCache.TryGet<string>(ELEMENT_KEY, out var element));
            Assert.AreEqual(ELEMENT_VALUE, element);

            Assert.AreSame(cache, otherCache);
        }

        [TestMethod]
        public void VerifyThatSharedCacheCanBeLoadedFromFileWhenKeyIsNotKnown()
        {
            //
            // Arrange
            //
            const string ELEMENT_KEY = "kii";
            const string ELEMENT_VALUE = "ads4s65ad4a6s8d4a8sd478asd4asd8546asd56";
            const string CACHE_KEY = nameof(VerifyThatSharedCacheCanBeLoadedFromFileWhenKeyIsNotKnown);

            using var cache = new ThreadCache(CACHE_KEY);
            _ = cache.TrySet(ELEMENT_KEY, ELEMENT_VALUE);

            using var tempFile = new TempFile();
            _ = cache.TrySaveToFile(tempFile.Info);

            //
            // Act
            //
            using ThreadCache? otherCache = ThreadCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Info);

            //
            // Assert
            //
            Assert.IsNotNull(otherCache);

            Assert.IsTrue(otherCache.TryGet<string>(ELEMENT_KEY, out var element));
            Assert.AreEqual(ELEMENT_VALUE, element);
        }

        [TestMethod]
        public void VerifyThatSharedCacheCanBeUpdatedFromFile()
        {
            //
            // Arrange
            //
            using ThreadCache cache = ThreadCache.Factory.GetOrCreateShared(nameof(VerifyThatSharedCacheCanBeUpdatedFromFile));

            const string KEY_1 = "Katter";
            const int VALUE_1 = 123;
            _ = cache.TrySet(KEY_1, VALUE_1);

            //
            // Act
            //
            using var tempFile = new TempFile();
            _ = cache.TrySaveToFile(tempFile.Info);

            _ = cache.TrySet(KEY_1, VALUE_1 * 2);

            const string KEY_2 = "Cat";
            const double VALUE_2 = 78.1234;
            _ = cache.TrySet(KEY_2, VALUE_2);

            ThreadCache? otherCache = ThreadCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Info);

            //
            // Assert
            //
            Assert.IsNotNull(otherCache);
            Assert.AreEqual(cache, otherCache);

            Assert.IsTrue(cache.TryGet<int>(KEY_1, out var updated));
            Assert.AreEqual(VALUE_1, updated);

            Assert.IsFalse(cache.TryGet<double>(KEY_2, out _));
        }

        [TestMethod]
        public void VerifyThatReadingSharedEmptyFileFails()
        {
            //
            // Arrange
            //
            using var tempFile = new TempFile();

            //
            // Act
            //
            using ThreadCache? cache = ThreadCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Info);

            //
            // Assert
            //
            Assert.IsNull(cache);
        }
    }
}
