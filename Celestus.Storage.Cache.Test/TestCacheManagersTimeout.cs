using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class CacheManagerHelperBaseTimeout
    {
        string Key { get; } = "test-key";
        string Item { get; } = "test-item";
        string Value { get; } = "test-value";

        [TestMethod]
        public void VerifyThatTryLoadThrowsLoadTimeoutExceptionWhenReadLockCannotBeAcquired()
        {
            //
            // Arrange
            //
            var manager = new CacheManagerHelper(CacheConstants.ShortDuration);

            //
            // Act & Assert
            //
            Assert.ThrowsException<LoadTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileWriteLocked(
                    () => manager.TryLoad(Key, out _)
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
                        _ = manager.GetOrCreateShared(Key, timeout: CacheConstants.VeryShortDuration);
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
            using (var cache = new Cache(Key))
            {
                cache.Set(Item, Value);
                _ = cache.TrySaveToFile(tempFile.Info);
            }

            //
            // Act & Assert
            //
            Assert.ThrowsException<SetFromFileTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileReadLocked(
                    () =>
                    {
                        _ = manager.UpdateOrLoadSharedFromFile(tempFile.Info, timeout: CacheConstants.VeryShortDuration);
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
                        manager.CallCacheExpired(Key);
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
                        _ = manager.TryLoad(Key, out _);
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
                        _ = manager.GetOrCreateShared(Key, timeout: CacheConstants.VeryShortDuration);
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
            using var cache = new ThreadCache(Key);

            _ = cache.TrySet(Item, Value);
            _ = cache.TrySaveToFile(tempFile.Info);

            //
            // Act & Assert
            //
            Assert.ThrowsException<SetFromFileTimeoutException>(
                () => ((IDoWhileLocked)manager).DoWhileReadLocked(
                    () =>
                    {
                        _ = manager.UpdateOrLoadSharedFromFile(tempFile.Info, timeout: CacheConstants.VeryShortDuration);
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
                        manager.CallCacheExpired(Key);
                        return true;
                    }
                )
            );
        }
    }
}