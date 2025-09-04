using Celestus.Io;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not thread safe since they dispose of resource other tests use.
    public sealed class TestThreadCacheSharedCreation
    {
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
            var otherResult = otherCache.TryGet<int>(KEY);

            //
            // Assert
            //
            Assert.AreEqual((true, VALUE), cache.TryGet<int>(KEY));
            Assert.AreEqual((true, VALUE + 1), otherResult);
        }

        [TestMethod]
        public void VerifyThatSharedCacheFailsWhenLoadedFromCorruptFile()
        {
            //
            // Arrange
            //
            var path = new Uri(Path.GetTempFileName());
            File.WriteAllText(path.AbsolutePath, "");

            //
            // Act
            //

            using var cache = ThreadCache.TryCreateFromFile(path);

            //
            // Assert
            //
            Assert.IsNull(cache);

            // Cleanup
            File.Delete(path.AbsolutePath);
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
            Assert.AreEqual((true, ELEMENT_VALUE), otherCache.TryGet<string>(ELEMENT_KEY));
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
            _ = cache.TrySaveToFile(tempFile.Uri);

            //
            // Act
            //
            using ThreadCache? otherCache = ThreadCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Uri);

            //
            // Assert
            //
            Assert.IsNotNull(otherCache);
            Assert.AreEqual((true, ELEMENT_VALUE), otherCache.TryGet<string>(ELEMENT_KEY));
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
            _ = cache.TrySaveToFile(tempFile.Uri);

            _ = cache.TrySet(KEY_1, VALUE_1 * 2);

            const string KEY_2 = "Cat";
            const double VALUE_2 = 78.1234;
            _ = cache.TrySet(KEY_2, VALUE_2);

            ThreadCache? otherCache = ThreadCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Uri);

            //
            // Assert
            //
            Assert.IsNotNull(otherCache);
            Assert.AreEqual(cache, otherCache);

            Assert.AreEqual(cache.TryGet<int>(KEY_1), (true, VALUE_1));
            Assert.AreEqual(cache.TryGet<double>(KEY_2), (false, 0));
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
            using ThreadCache? cache = ThreadCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Uri);

            //
            // Assert
            //
            Assert.IsNull(cache);
        }
    }
}
