using System.Threading;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not thread safe since they dispose of resource other tests use.
    public sealed class TestThreadCacheTimeout
    {
        const int THREAD_TIMEOUT = 1000;

        private ThreadCache _cache = null!;

        [TestInitialize]
        public void Initialize()
        {
            _cache = ThreadCache.GetOrCreateShared(nameof(TestThreadCacheTimeout));
        }

        [TestMethod]
        public void VerifyThatSetCanTimeout()
        {
            //
            // Arrange
            //
            const string KEY = "Sjö";
            _cache.TrySet(KEY, 1);

            //
            // Act
            //
            var setResult = ThreadTimeout.DoWhileLocked(_cache, () => _cache.TrySet(KEY, 0, timeout: 1), THREAD_TIMEOUT);

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
            const string KEY = "Sjö";
            _cache.TrySet(KEY, 1);

            //
            // Act
            //
            var getResult = ThreadTimeout.DoWhileLocked(_cache, () => _cache.TryGet<int>(KEY, timeout: 1), THREAD_TIMEOUT);

            //
            // Assert
            //
            Assert.AreEqual((false, default), getResult);
        }

        [TestMethod]
        public void VerifyThatUpdatingSharedCacheCanTimeout()
        {
            //
            // Arrange
            //
            const string KEY = "Sjö";
            _cache.TrySet(KEY, 1);

            var path = new Uri(Path.GetTempFileName());
            _cache.SaveToFile(path);

            //
            // Act
            //
            var loadedCache = ThreadTimeout.DoWhileLocked(_cache, () => ThreadCache.UpdateOrLoadSharedFromFile(path, timeout: 1), THREAD_TIMEOUT);

            //
            // Assert
            //
            Assert.IsNull(loadedCache);
        }
    }
}
