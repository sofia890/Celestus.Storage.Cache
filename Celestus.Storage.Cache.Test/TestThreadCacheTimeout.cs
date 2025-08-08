using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public sealed class TestThreadCacheTimeout
    {
        const int THREAD_TIMEOUT = 1000;

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
            var setResult = ThreadHelper.DoWhileLocked(cache, () => cache.TrySet(KEY, 0, timeout: 1), THREAD_TIMEOUT);

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
            var getResult = ThreadHelper.DoWhileLocked(cache, () => cache.TryGet<int>(KEY, timeout: 1), THREAD_TIMEOUT);

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
            var getResult = ThreadHelper.DoWhileLocked(cache, () => cache.TryRemove([KEY], timeout: 1), THREAD_TIMEOUT);

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
            using var cache = ThreadCacheManager.GetOrCreateShared(nameof(VerifyThatUpdatingSharedCacheCanTimeout));

            const string KEY = "Sjö";
            cache.TrySet(KEY, 1);

            var path = new Uri(Path.GetTempFileName());
            cache.TrySaveToFile(path);

            //
            // Act
            //
            var loadedCache = ThreadHelper.DoWhileLocked(cache, () => ThreadCacheManager.UpdateOrLoadSharedFromFile(path, timeout: 1), THREAD_TIMEOUT);

            //
            // Assert
            //
            Assert.IsNull(loadedCache);

            // Cleanup
            File.Delete(path.AbsolutePath);
        }
    }
}
