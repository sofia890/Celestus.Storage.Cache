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
            var cache = new ThreadCache();

            const string KEY = "Sjö";
            cache.TrySet(KEY, 1);

            //
            // Act
            //
            var setResult = ThreadTimeout.DoWhileLocked(cache, () => cache.TrySet(KEY, 0, timeout: 1), THREAD_TIMEOUT);

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
            var cache = new ThreadCache();

            const string KEY = "Sjö";
            cache.TrySet(KEY, 1);

            //
            // Act
            //
            var getResult = ThreadTimeout.DoWhileLocked(cache, () => cache.TryGet<int>(KEY, timeout: 1), THREAD_TIMEOUT);

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
            var cache = new ThreadCache();

            const string KEY = "Lake";
            cache.TrySet(KEY, 1);

            //
            // Act
            //
            var getResult = ThreadTimeout.DoWhileLocked(cache, () => cache.TryRemove([KEY], timeout: 1), THREAD_TIMEOUT);

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
            var cache = ThreadCache.GetOrCreateShared(nameof(VerifyThatUpdatingSharedCacheCanTimeout));

            const string KEY = "Sjö";
            cache.TrySet(KEY, 1);

            var path = new Uri(Path.GetTempFileName());
            cache.SaveToFile(path);

            //
            // Act
            //
            var loadedCache = ThreadTimeout.DoWhileLocked(cache, () => ThreadCache.UpdateOrLoadSharedFromFile(path, timeout: 1), THREAD_TIMEOUT);

            //
            // Assert
            //
            Assert.IsNull(loadedCache);
        }
    }
}
