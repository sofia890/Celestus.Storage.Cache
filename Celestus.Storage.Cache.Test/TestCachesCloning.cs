using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestCachesCloning
    {
        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadSafeCache))]
        public void VerifyThatToCacheCreatesIndependentCopy(Type cacheType)
        {
            //
            // Arrange
            //
            using var originalCache = CacheHelper.Create(cacheType, string.Empty);
            originalCache.Set("key1", 42);
            originalCache.Set("key2", "test");

            //
            // Act
            //
            using var clonedCache = (CacheBase<string, string>)originalCache.Clone();

            //
            // Assert
            //
            Assert.AreEqual(originalCache, clonedCache);
            Assert.AreNotSame(originalCache, clonedCache);
            Assert.AreNotSame(originalCache.Storage, clonedCache.Storage);
        }

        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadSafeCache))]
        public void VerifyThatClonesAreIndependent(Type cacheType)
        {
            //
            // Arrange
            //
            using var originalCache = CacheHelper.Create(cacheType, string.Empty);

            const string SHARED_KEY = "shared";
            const string SHARED_ORIGINAL_VALUE = "original";
            originalCache.Set(SHARED_KEY, SHARED_ORIGINAL_VALUE);

            using var clonedCache = (CacheBase<string, string>)originalCache.Clone();

            //
            // Act
            //
            const string SHARED_CLONE_VALUE = "modified";
            originalCache.Set(SHARED_KEY, SHARED_CLONE_VALUE);

            const string ORIGINAL_ONLY_KEY = "original-only";
            const string ORIGINAL_ONLY_VALUE = "value-original";
            originalCache.Set(ORIGINAL_ONLY_KEY, ORIGINAL_ONLY_VALUE);

            const string CLONE_ONLY_KEY = "clone-only";
            const string CLONE_ONLY_VALUE = "value-clone";
            clonedCache.Set(CLONE_ONLY_KEY, CLONE_ONLY_VALUE);

            //
            // Assert
            //
            Assert.IsTrue(originalCache.TryGet<string>(SHARED_KEY, out var originalData));
            Assert.AreEqual(SHARED_CLONE_VALUE, originalData);

            Assert.IsTrue(clonedCache.TryGet<string>(SHARED_KEY, out var clonedData));
            Assert.AreEqual(SHARED_ORIGINAL_VALUE, clonedData);

            Assert.IsTrue(originalCache.TryGet<string>(ORIGINAL_ONLY_KEY, out _));
            Assert.IsFalse(clonedCache.TryGet<string>(ORIGINAL_ONLY_KEY, out _));

            Assert.IsFalse(originalCache.TryGet<string>(CLONE_ONLY_KEY, out _));
            Assert.IsTrue(clonedCache.TryGet<string>(CLONE_ONLY_KEY, out _));
        }

        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadSafeCache))]
        public void VerifyThatICloneableCloneWorks(Type cacheType)
        {
            //
            // Arrange
            //
            using var originalCache = CacheHelper.Create(cacheType, string.Empty);
            originalCache.Set("test", 123);

            //
            // Act
            //
            var clonedObject = ((ICloneable)originalCache).Clone();

            //
            // Assert
            //
            Assert.AreEqual(cacheType, clonedObject.GetType());

            var clonedCache = (CacheBase<string, string>)clonedObject;
            Assert.AreEqual(originalCache, clonedCache);
            Assert.AreNotSame(originalCache, clonedCache);

            clonedCache.Dispose();
        }

        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadSafeCache))]
        public void VerifyThatClonedCacheCanBeModifiedIndependently(Type cacheType)
        {
            //
            // Arrange
            //
            using var originalCache = CacheHelper.Create(cacheType, string.Empty);

            const string KEY_1 = "Key1";
            originalCache.Set(KEY_1, 1);

            const string KEY_2 = "Key2";
            originalCache.Set(KEY_2, 2);

            using var clonedCache = (CacheBase<string, string>)originalCache.Clone();

            //
            // Act
            //
            clonedCache.TryRemove([KEY_1]);

            const string KEY_3 = "Key3";
            clonedCache.Set(KEY_3, 3);

            //
            // Assert
            //
            // Original should be unchanged
            Assert.IsTrue(originalCache.TryGet<int>(KEY_1, out _));
            Assert.IsTrue(originalCache.TryGet<int>(KEY_2, out _));
            Assert.IsFalse(originalCache.TryGet<int>(KEY_3, out _));

            // Clone should have changes
            Assert.IsFalse(clonedCache.TryGet<int>(KEY_1, out _));
            Assert.IsTrue(clonedCache.TryGet<int>(KEY_2, out _));
            Assert.IsTrue(clonedCache.TryGet<int>(KEY_3, out _));
        }

        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadSafeCache))]
        public void VerifyThatEmptyCacheClonedCorrectly(Type cacheType)
        {
            //
            // Arrange
            //
            using var originalCache = CacheHelper.Create(cacheType, string.Empty);

            //
            // Act
            //
            using var clonedCache = (CacheBase<string, string>)originalCache.Clone();

            //
            // Assert
            //
            Assert.AreEqual(originalCache, clonedCache);
            Assert.AreNotSame(originalCache, clonedCache);
            Assert.AreEqual(0, clonedCache.Storage.Count);
        }
    }
}