namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not thread safe since they dispose of resource other tests use.
    public sealed class TestThreadCacheSharedCreation
    {
        [TestMethod]
        public void VerifyThatSharedCacheCreatesKeyWhenNotProvided()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            var cache = ThreadCache.GetOrCreateShared();

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

            //
            // Act
            //
            var path = new Uri(Path.GetTempFileName());
            File.WriteAllText(path.AbsolutePath, "");

            var cache = ThreadCache.TryCreateFromFile(path);

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

            var cache = ThreadCache.GetOrCreateShared(CACHE_KEY);
            _ = cache.TrySet(ELEMENT_KEY, ELEMENT_VALUE);

            //
            // Act
            //
            var otherCache = ThreadCache.GetOrCreateShared(CACHE_KEY);

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

            var cache = new ThreadCache(CACHE_KEY);
            _ = cache.TrySet(ELEMENT_KEY, ELEMENT_VALUE);

            var path = new Uri(Path.GetTempFileName());
            cache.SaveToFile(path);

            //
            // Act
            //
            var otherCache = ThreadCache.UpdateOrLoadSharedFromFile(path);

            File.Delete(path.AbsolutePath);

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
            ThreadCache cache = ThreadCache.GetOrCreateShared(nameof(VerifyThatSharedCacheCanBeUpdatedFromFile));

            const string KEY_1 = "Katter";
            const int VALUE_1 = 123;
            _ = cache.TrySet(KEY_1, VALUE_1);

            //
            // Act
            //
            var path = new Uri(Path.GetTempFileName());
            cache.SaveToFile(path);

            _ = cache.TrySet(KEY_1, VALUE_1 * 2);

            const string KEY_2 = "Neko";
            const double VALUE_2 = 78.1234;
            _ = cache.TrySet(KEY_2, VALUE_2);

            ThreadCache? otherCache = ThreadCache.UpdateOrLoadSharedFromFile(path);

            File.Delete(path.AbsolutePath);

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
            var path = new Uri(Path.GetTempFileName());
            File.WriteAllText(path.AbsolutePath, "");

            //
            // Act
            //
            bool loaded = ThreadCache.UpdateOrLoadSharedFromFile(path) != null;

            File.Delete(path.AbsolutePath);

            //
            // Assert
            //
            Assert.IsFalse(loaded);
        }
    }
}
