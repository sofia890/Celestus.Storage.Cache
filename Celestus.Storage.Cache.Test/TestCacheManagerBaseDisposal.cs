using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestCacheManagerBaseDisposal
    {
        [TestMethod]
        public void VerifyThatTryLoadThrowsObjectDisposedExceptionAfterDisposal()
        {
            //
            // Arrange
            //
            var manager = new Cache.CacheManager();
            manager.Dispose();

            //
            // Act & Assert
            //
            Assert.ThrowsException<ObjectDisposedException>(() => manager.TryLoad("test-key", out _));
        }

        [TestMethod]
        public void VerifyThatGetOrCreateSharedThrowsObjectDisposedExceptionAfterDisposal()
        {
            //
            // Arrange
            //
            var manager = new Cache.CacheManager();
            manager.Dispose();

            //
            // Act & Assert
            //
            Assert.ThrowsException<ObjectDisposedException>(() => manager.GetOrCreateShared("test-key"));
        }

        [TestMethod]
        public void VerifyThatGetOrCreateSharedWithEmptyKeyThrowsObjectDisposedExceptionAfterDisposal()
        {
            //
            // Arrange
            //
            var manager = new Cache.CacheManager();
            manager.Dispose();

            //
            // Act & Assert
            //
            Assert.ThrowsException<ObjectDisposedException>(() => manager.GetOrCreateShared());
        }

        [TestMethod]
        public void VerifyThatUpdateOrLoadSharedFromFileThrowsObjectDisposedExceptionAfterDisposal()
        {
            //
            // Arrange
            //
            var manager = new Cache.CacheManager();
            
            // Create a valid test file
            using var tempFile = new TempFile();
            using (var cache = new Cache("test-key"))
            {
                cache.Set("test-item", "test-value");
                cache.SaveToFile(tempFile.Uri);
            }

            manager.Dispose();

            //
            // Act & Assert
            //
            Assert.ThrowsException<ObjectDisposedException>(() => manager.UpdateOrLoadSharedFromFile(tempFile.Uri));
        }

        [TestMethod]
        public void VerifyThatCacheExpiredThrowsObjectDisposedExceptionAfterDisposal()
        {
            //
            // Arrange
            //
            var manager = new Cache.CacheManager();
            manager.Dispose();

            //
            // Act & Assert
            //
            Assert.ThrowsException<ObjectDisposedException>(() => manager.CacheExpired("test-key"));
        }

        [TestMethod]
        public void VerifyThatThreadCacheManagerTryLoadThrowsObjectDisposedExceptionAfterDisposal()
        {
            //
            // Arrange
            //
            var manager = new ThreadCache.ThreadCacheManager();
            manager.Dispose();

            //
            // Act & Assert
            //
            Assert.ThrowsException<ObjectDisposedException>(() => manager.TryLoad("test-key", out _));
        }

        [TestMethod]
        public void VerifyThatThreadCacheManagerGetOrCreateSharedThrowsObjectDisposedExceptionAfterDisposal()
        {
            //
            // Arrange
            //
            var manager = new ThreadCache.ThreadCacheManager();
            manager.Dispose();

            //
            // Act & Assert
            //
            Assert.ThrowsException<ObjectDisposedException>(() => manager.GetOrCreateShared("test-key"));
        }

        [TestMethod]
        public void VerifyThatThreadCacheManagerUpdateOrLoadSharedFromFileThrowsObjectDisposedExceptionAfterDisposal()
        {
            //
            // Arrange
            //
            var manager = new ThreadCache.ThreadCacheManager();
            
            // Create a valid test file
            using var tempFile = new TempFile();
            using var cache = new ThreadCache("test-key");

            _ = cache.TrySet("test-item", "test-value");
            cache.TrySaveToFile(tempFile.Uri);

            manager.Dispose();

            //
            // Act & Assert
            //
            Assert.ThrowsException<ObjectDisposedException>(() => manager.UpdateOrLoadSharedFromFile(tempFile.Uri));
        }

        [TestMethod]
        public void VerifyThatThreadCacheManagerCacheExpiredThrowsObjectDisposedExceptionAfterDisposal()
        {
            //
            // Arrange
            //
            var manager = new ThreadCache.ThreadCacheManager();
            manager.Dispose();

            //
            // Act & Assert
            //
            Assert.ThrowsException<ObjectDisposedException>(() => manager.CacheExpired("test-key"));
        }
    }
}