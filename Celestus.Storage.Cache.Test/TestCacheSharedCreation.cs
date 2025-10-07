using Celestus.Io;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not thread safe since they dispose of resource other tests use.
    public sealed class TestCacheSharedCreation
    {
        [TestMethod]
        public void VerifyThatLoadingFromNonExistentFileReturnsNull()
        {
            //
            // Arrange
            //
            var nonExistentFile = new FileInfo("C:/Users/so/non-existent-file.json");

            //
            // Act
            //
            var cache = Cache.TryCreateFromFile(nonExistentFile);

            //
            // Assert
            //
            Assert.IsNull(cache);
        }

        [TestMethod]
        public void VerifyThatSharedCacheFailsWhenLoadedFromCorruptFile()
        {
            //
            // Arrange
            //
            var file = new FileInfo(Path.GetTempFileName());
            File.WriteAllText(file.FullName, "");

            //
            // Act
            //
            using var cache = Cache.TryCreateFromFile(file);

            //
            // Assert
            //
            Assert.IsNull(cache);

            // Cleanup
            File.Delete(file.FullName);
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

            using var cache = Cache.Factory.GetOrCreateShared(CACHE_KEY);
            cache.Set(ELEMENT_KEY, ELEMENT_VALUE);

            //
            // Act
            //
            using var otherCache = Cache.Factory.GetOrCreateShared(CACHE_KEY);

            //
            // Assert
            //
            Assert.IsTrue(otherCache.TryGet<string>(ELEMENT_KEY, out var value));
            Assert.AreEqual(ELEMENT_VALUE, value);
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

            using var cache = new Cache(CACHE_KEY);
            cache.Set(ELEMENT_KEY, ELEMENT_VALUE);

            using var tempFile = new TempFile();
            _ = cache.TrySaveToFile(tempFile.Info);

            //
            // Act
            //
            using var otherCache = Cache.Factory.UpdateOrLoadSharedFromFile(tempFile.Info);

            //
            // Assert
            //
            Assert.IsNotNull(otherCache);
            Assert.IsTrue(otherCache.TryGet<string>(ELEMENT_KEY, out var value));
            Assert.AreEqual(ELEMENT_VALUE, value);
        }

        [TestMethod]
        public void VerifyThatSharedCacheCanBeUpdatedFromFile()
        {
            //
            // Arrange
            //
            using Cache cache = Cache.Factory.GetOrCreateShared(nameof(VerifyThatSharedCacheCanBeUpdatedFromFile));

            const string KEY_1 = "Katter";
            const int VALUE_1 = 123;

            var longDuration = TimeSpan.FromHours(1);
            cache.Set(KEY_1, VALUE_1, duration: longDuration);

            //
            // Act
            //
            using var tempFile = new TempFile();
            _ = cache.TrySaveToFile(tempFile.Info);

            cache.Set(KEY_1, VALUE_1 * 2, duration: longDuration);

            const string KEY_2 = "Snake";
            const double VALUE_2 = 78.1234;
            cache.Set(KEY_2, VALUE_2, duration: longDuration);

            using Cache? otherCache = Cache.Factory.UpdateOrLoadSharedFromFile(tempFile.Info);

            //
            // Assert
            //
            Assert.IsNotNull(otherCache);
            Assert.AreEqual(cache, otherCache);
            Assert.IsTrue(cache.TryGet<int>(KEY_1, out var value1));
            Assert.AreEqual(VALUE_1, value1);

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
            using var cache = Cache.TryCreateFromFile(tempFile.Info);

            //
            // Assert
            //
            Assert.IsNull(cache);
        }
    }
}
