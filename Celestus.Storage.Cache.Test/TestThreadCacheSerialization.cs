namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not thread safe since they dispose of resource other tests use.
    public sealed class TestThreadCacheSerialization
    {
        private ThreadCache _cache = null!;

        [TestInitialize]
        public void Initialize()
        {
            _cache = ThreadCache.GetOrCreateShared(nameof(TestThreadCacheReadingAndWriting));
        }

        [TestMethod]
        public void VerifyThatSerializationToAndFromFileWorks()
        {
            //
            // Arrange
            //
            const string KEY_1 = "serial";
            const int VALUE_1 = 57;
            _ = _cache.TrySet(KEY_1, VALUE_1);

            const string KEY_2 = "Mattis";
            const double VALUE_2 = 11.5679;
            _ = _cache.TrySet(KEY_2, VALUE_2);

            const string KEY_3 = "Lakris";
            DateTime VALUE_3 = DateTime.Now;
            _ = _cache.TrySet(KEY_3, VALUE_3);

            const string KEY_4 = "Ludde";
            ExampleRecord VALUE_4 = new(-9634, "VerifyThatSerializationWorks", 10000000M);
            _ = _cache.TrySet(KEY_4, VALUE_4);

            //
            // Act
            //
            var path = new Uri(Path.GetTempFileName());
            _cache.SaveToFile(path);

            ThreadCache? otherCache = ThreadCache.TryCreateFromFile(path);

            File.Delete(path.AbsolutePath);

            //
            // Assert
            //
            Assert.IsNotNull(otherCache);
            Assert.AreNotEqual(string.Empty, otherCache.Key);
            Assert.AreEqual(_cache, otherCache);
            Assert.AreEqual(_cache.GetHashCode(), otherCache.GetHashCode());

            Assert.AreEqual(otherCache.TryGet<int>(KEY_1), (true, VALUE_1));
            Assert.AreEqual(otherCache.TryGet<double>(KEY_2), (true, VALUE_2));
            Assert.AreEqual(otherCache.TryGet<DateTime>(KEY_3), (true, VALUE_3));
            Assert.AreEqual(otherCache.TryGet<ExampleRecord>(KEY_4), (true, VALUE_4));
        }

        [TestMethod]
        public void VerifyThatCacheIsNotLoadedWhenKeysDiffer()
        {
            //
            // Arrange
            //
            ThreadCache cacheOne = new("SomeKey");

            const string KEY_1 = "Janta";
            const Decimal VALUE_1 = 598888899663145M;
            _ = cacheOne.TrySet(KEY_1, VALUE_1);

            var path = new Uri(Path.GetTempFileName());
            cacheOne.SaveToFile(path);

            //
            // Act
            //
            ThreadCache cacheTwo = new("anotherKey");
            bool loaded = cacheTwo.TryLoadFromFile(path);

            //
            // Assert
            //
            Assert.IsFalse(loaded);
            Assert.AreEqual(cacheTwo.TryGet<int>(KEY_1), (false, default));
        }

        [TestMethod]
        public void VerifyThatCacheCanBeUpdatedFromFile()
        {
            //
            // Arrange
            //
            ThreadCache cache = new();

            const string KEY_1 = "Katter";
            const int VALUE_1 = 123;
            _ = cache.TrySet(KEY_1, VALUE_1);

            //
            // Act
            //
            var path = new Uri(Path.GetTempFileName());
            cache.SaveToFile(path);

            _ = cache.TrySet(KEY_1, VALUE_1 * 2);

            const string KEY_2 = "Explain";
            const double VALUE_2 = 78.1234;
            _ = cache.TrySet(KEY_2, VALUE_2);

            bool loaded = cache.TryLoadFromFile(path);

            //
            // Assert
            //
            Assert.IsTrue(loaded);
            Assert.AreEqual(cache.TryGet<int>(KEY_1), (true, VALUE_1));
            Assert.AreEqual(cache.TryGet<double>(KEY_2), (false, 0));
        }

        [TestMethod]
        public void VerifyThatReadingEmptyFileFails()
        {
            //
            // Arrange
            //
            ThreadCache cache = new();

            var path = new Uri(Path.GetTempFileName());
            File.WriteAllText(path.AbsolutePath, "");

            //
            // Act
            //
            bool loaded = cache.TryLoadFromFile(path);

            File.Delete(path.AbsolutePath);

            //
            // Assert
            //
            Assert.IsFalse(loaded);
        }
    }
}
