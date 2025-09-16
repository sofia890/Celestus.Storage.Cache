using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestCachePersistence
    {
        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadCache))]
        public void VerifyThatPersistentCacheSavesOnDispose(Type cacheTypeToTest)
        {
            //
            // Arrange
            //
            using var tempFile = new TempFile();

            var oneDay = TimeSpan.FromDays(1);

            const string CACHE_KEY = nameof(VerifyThatPersistentCacheSavesOnDispose);
            var cache = CacheHelper.Create(cacheTypeToTest, CACHE_KEY, persistent: true, tempFile.Uri.AbsolutePath);

            const string KEY_A = "A";
            const int VALUE_A = 123;
            cache.Set(KEY_A, VALUE_A, oneDay);

            const string KEY_B = "B";
            const string VALUE_B = "test";
            cache.Set(KEY_B, VALUE_B, oneDay);

            //
            // Act
            //
            cache.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            //
            // Assert
            //
            var loaded = CacheHelper.Create(cacheTypeToTest, CACHE_KEY, persistent: true, tempFile.Uri.AbsolutePath);
            Assert.AreEqual(VALUE_A, loaded.Get<int>(KEY_A));
            Assert.AreEqual(VALUE_B, loaded.Get<string>(KEY_B));

            // Need to dispose to trigger persistence logic before the tempo file is removed.
            // Test runner has limited access to file system. Once file is deleted said runner
            // cannot recreate the file by just writing to it.
            loaded.Dispose();
        }

        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadCache))]
        public void VerifyThatPersistentCacheMismatchThrowsException(Type cacheTypeToTest)
        {
            //
            // Arrange
            //
            using var tempFile = new TempFile();
            const string CACHE_KEY = nameof(VerifyThatPersistentCacheSavesOnDispose);
            var cacheA = CacheHelper.GetOrCreateShared(cacheTypeToTest, CACHE_KEY, persistent: true, tempFile.Uri.AbsolutePath);

            //
            // Act & Assert
            //
            Assert.ThrowsException<PersistenceMismatchException>(() => CacheHelper.GetOrCreateShared(cacheTypeToTest, CACHE_KEY));

            // Need to dispose to trigger persistence logic before the tempo file is removed.
            // Test runner has limited access to file system. Once file is deleted said runner
            // cannot recreate the file by just writing to it.
            cacheA.Dispose();
        }
    }
}
