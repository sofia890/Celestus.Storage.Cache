using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;
using System.Reflection;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize]
    public class CacheManagerHelperBaseTimeout
    {
        [TestMethod]
        [DataRow(typeof(CacheManagerHelper))]
        [DataRow(typeof(ThreadCacheManagerHelper))]
        public void VerifyThatTryLoadThrowsLoadTimeoutExceptionWhenReadLockCannotBeAcquired(Type cacheManagerTypeToTest)
        {
            //
            // Arrange
            //
            var manager =new CacheManagerHelper(CacheConstants.ShortDuration);

            //
            // Act & Assert
            //
            Assert.ThrowsException<LoadTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileWriteLocked(
                    () => manager.TryLoad("test-key", out _)
                )
            );
        }

        [TestMethod]
        public void VerifyThatGetOrCreateSharedThrowsSetTimeoutExceptionWhenWriteLockCannotBeAcquired()
        {
            //
            // Arrange
            //
            var manager = new CacheManagerHelper(CacheConstants.ShortDuration);

            //
            // Act & Assert
            //
            Assert.ThrowsException<SetTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileReadLocked(
                    () =>
                    {
                        _ = manager.GetOrCreateShared("test-key");
                        return true;
                    }
                )
            );
        }

        [TestMethod]
        public void VerifyThatUpdateOrLoadSharedFromFileThrowsSetFromFileTimeoutExceptionWhenWriteLockCannotBeAcquired()
        {
            //
            // Arrange
            //
            var manager = new CacheManagerHelper(CacheConstants.ShortDuration);

            // Create a valid test file
            using var tempFile = new TempFile();
            using (var cache = new Cache("test-key"))
            {
                cache.Set("test-item", "test-value");
                _ = cache.TrySaveToFile(tempFile.Uri);
            }

            //
            // Act & Assert
            //
            Assert.ThrowsException<SetFromFileTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileReadLocked(
                    () =>
                    {
                        _ = manager.UpdateOrLoadSharedFromFile(tempFile.Uri);
                        return true;
                    }
                )
            );
        }

        [TestMethod]
        public void VerifyThatCacheExpiredThrowsCleanupTimeoutExceptionWhenWriteLockCannotBeAcquired()
        {
            //
            // Arrange
            //
            var manager = new CacheManagerHelper(CacheConstants.ShortDuration);

            //
            // Act & Assert
            //
            Assert.ThrowsException<CleanupTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileReadLocked(
                    () =>
                    {
                        manager.CallCacheExpired("test-key");
                        return true;
                    }
                )
            );
        }

        [TestMethod]
        public void VerifyThatThreadCacheTryLoadThrowsLoadTimeoutExceptionWhenReadLockCannotBeAcquired()
        {
            //
            // Arrange
            //
            using var manager = new ThreadCacheManagerHelper(CacheConstants.ShortDuration);

            //
            // Act & Assert
            //
            Assert.ThrowsException<LoadTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileWriteLocked(
                    () =>
                    {
                        _ = manager.TryLoad("test-key", out _);
                        return true;
                    }
                )
            );
        }

        [TestMethod]
        public void VerifyThatThreadCacheGetOrCreateSharedThrowsSetTimeoutExceptionWhenWriteLockCannotBeAcquired()
        {
            //
            // Arrange
            //
            using var manager = new ThreadCacheManagerHelper(CacheConstants.ShortDuration);

            //
            // Act & Assert
            //
            Assert.ThrowsException<SetTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileReadLocked(
                    () =>
                    {
                        _ = manager.GetOrCreateShared("test-key");
                        return true;
                    }
                )
            );
        }

        [TestMethod]
        public void VerifyThatThreadCacheUpdateOrLoadSharedFromFileThrowsSetFromFileTimeoutExceptionWhenWriteLockCannotBeAcquired()
        {
            //
            // Arrange
            //
            using var manager = new ThreadCacheManagerHelper(CacheConstants.ShortDuration);

            // Create a valid test file
            using var tempFile = new TempFile();
            using var cache = new ThreadCache("test-key");

            _ = cache.TrySet("test-item", "test-value");
            _ = cache.TrySaveToFile(tempFile.Uri);

            //
            // Act & Assert
            //
            Assert.ThrowsException<SetFromFileTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileReadLocked(
                    () =>
                    {
                        _ = manager.UpdateOrLoadSharedFromFile(tempFile.Uri);
                        return true;
                    }
                )
            );
        }

        [TestMethod]
        public void VerifyThatThreadCacheCacheExpiredThrowsCleanupTimeoutExceptionWhenWriteLockCannotBeAcquired()
        {
            //
            // Arrange
            //
            using var manager = new ThreadCacheManagerHelper(CacheConstants.ShortDuration);

            //
            // Act & Assert
            //
            Assert.ThrowsException<CleanupTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileReadLocked(
                    () =>
                    {
                        manager.CallCacheExpired("test-key");
                        return true;
                    }
                )
            );
        }
    }
}