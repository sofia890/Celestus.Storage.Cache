using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestCachesDisposal
    {
        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadCache))]
        public void VerifyThatCacheIsDisposedStateIsCorrect(Type cacheType)
        {
            //
            // Arrange
            //
            var cache = CacheHelper.Create(cacheType, string.Empty);

            //
            // Act & Assert
            //
            Assert.IsFalse(cache.IsDisposed);
            cache.Dispose();
            Assert.IsTrue(cache.IsDisposed);
        }

        [TestMethod]
        [DataRow(typeof(Cache))]
        [DataRow(typeof(ThreadCache))]
        public void VerifyThatMultipleDisposeCallsAreSafe(Type cacheType)
        {
            //
            // Arrange
            //
            var cache = CacheHelper.Create(cacheType, string.Empty);

            //
            // Act & Assert (No exception should be thrown)
            //
            cache.Dispose();
            cache.Dispose();
            cache.Dispose();
            Assert.IsTrue(cache.IsDisposed);
        }
    }
}