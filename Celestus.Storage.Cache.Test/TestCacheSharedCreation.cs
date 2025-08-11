namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not thread safe since they dispose of resource other tests use.
    public sealed class TestCacheSharedCreation
    {
        [TestMethod]
        public void VerifyThatSharedCacheCreatesKeyWhenNotProvided()
        {
            //
            // Arrange & Act
            //
            using var cache = CacheManager.GetOrCreateShared();

            //
            // Assert
            //
            Assert.AreNotEqual(default, cache.Key);
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
            using var cache = Cache.TryCreateFromFile(path);

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

            using var cache = CacheManager.GetOrCreateShared(CACHE_KEY);
            cache.Set(ELEMENT_KEY, ELEMENT_VALUE);

            //
            // Act
            //
            using var otherCache = CacheManager.GetOrCreateShared(CACHE_KEY);

            //
            // Assert
            //
            Assert.AreEqual((true, ELEMENT_VALUE), otherCache.TryGet<string>(ELEMENT_KEY));
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

            var path = new Uri(Path.GetTempFileName());
            cache.SaveToFile(path);

            //
            // Act
            //
            using var otherCache = CacheManager.UpdateOrLoadSharedFromFile(path);

            //
            // Assert
            //
            Assert.IsNotNull(otherCache);
            Assert.AreEqual((true, ELEMENT_VALUE), otherCache.TryGet<string>(ELEMENT_KEY));

            // Cleanup
            File.Delete(path.AbsolutePath);
        }

        [TestMethod]
        public void VerifyThatSharedCacheCanBeUpdatedFromFile()
        {
            //
            // Arrange
            //
            using Cache cache = CacheManager.GetOrCreateShared(nameof(VerifyThatSharedCacheCanBeUpdatedFromFile));

            const string KEY_1 = "Katter";
            const int VALUE_1 = 123;
            cache.Set(KEY_1, VALUE_1);

            //
            // Act
            //
            var path = new Uri(Path.GetTempFileName());
            cache.SaveToFile(path);

            cache.Set(KEY_1, VALUE_1 * 2);

            const string KEY_2 = "Snake";
            const double VALUE_2 = 78.1234;
            cache.Set(KEY_2, VALUE_2);

            using Cache? otherCache = CacheManager.UpdateOrLoadSharedFromFile(path);

            //
            // Assert
            //
            Assert.IsNotNull(otherCache);
            Assert.AreEqual(cache, otherCache);
            Assert.AreEqual(cache.TryGet<int>(KEY_1), (true, VALUE_1));
            Assert.AreEqual(cache.TryGet<double>(KEY_2), (false, 0));

            // Cleanup
            File.Delete(path.AbsolutePath);
        }

        [TestMethod]
        public void VerifyThatReadingSharedEmptyFileFails()
        {
            //
            // Arrange
            //
            var path = new Uri(Path.GetTempFileName());
            File.WriteAllText(path.AbsolutePath, "");

            //
            // Act
            //
            using var cache = Cache.TryCreateFromFile(path);

            //
            // Assert
            //
            Assert.IsNull(cache);

            // Cleanup
            File.Delete(path.AbsolutePath);
        }
    }
}
