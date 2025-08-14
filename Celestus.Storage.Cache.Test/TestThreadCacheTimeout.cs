using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public sealed class TestThreadCacheTimeout
    {
        [TestMethod]
        public void VerifyThatSetCanTimeout()
        {
            //
            // Arrange
            //
            using var cache = new ThreadCache();

            const string KEY = "Sjö";
            cache.TrySet(KEY, 1);

            //
            // Act
            //
            var setResult = ThreadHelper.DoWhileLocked(
                cache,
                () => cache.TrySet(KEY, 0, timeout: CacheConstants.VeryShortDuration),
                CacheConstants.ShortDuration
            );

            //
            // Assert
            //
            Assert.IsFalse(setResult);
        }

        [TestMethod]
        public void VerifyThatGetCanTimeout()
        {
            //
            // Arrange
            //
            using var cache = new ThreadCache();

            const string KEY = "Sjö";
            cache.TrySet(KEY, 1);

            //
            // Act
            //
            var getResult = ThreadHelper.DoWhileLocked(
                cache,
                () => cache.TryGet<int>(KEY, timeout: CacheConstants.VeryShortDuration),
                CacheConstants.ShortDuration
            );

            //
            // Assert
            //
            Assert.AreEqual((false, default), getResult);
        }

        [TestMethod]
        public void VerifyThatRemoveCanTimeout()
        {
            //
            // Arrange
            //
            using var cache = new ThreadCache();

            const string KEY = "Lake";
            cache.TrySet(KEY, 1);

            //
            // Act
            //
            var getResult = ThreadHelper.DoWhileLocked(
                cache,
                () => cache.TryRemove([KEY], timeout: CacheConstants.VeryShortDuration),
                CacheConstants.ShortDuration
            );

            //
            // Assert
            //
            Assert.IsFalse(getResult);
        }

        [TestMethod]
        public void VerifyThatUpdatingSharedCacheCanTimeout()
        {
            //
            // Arrange
            //
            using var cache = ThreadCache.Factory.GetOrCreateShared(nameof(VerifyThatUpdatingSharedCacheCanTimeout));

            const string KEY = "Sjö";
            cache.TrySet(KEY, 1);

            using var tempFile = new TempFile();
            cache.TrySaveToFile(tempFile.Uri);

            //
            // Act
            //
            var loadedCache = ThreadHelper.DoWhileLocked(
                cache,
                () => ThreadCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Uri, timeout: TimeSpan.FromMilliseconds(1)),
                CacheConstants.ShortDuration
              );

            //
            // Assert
            //
            Assert.IsNull(loadedCache);
        }
    }
}
