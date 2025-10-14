using Celestus.Io;
using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public sealed class TestThreadSafeCacheTimeout
    {
        [TestMethod]
        public void VerifyThatSetCanTimeout()
        {
            using var cache = new ThreadSafeCache();
            const string KEY = "Sjö"; cache.TrySet(KEY, 1);
            var setResult = ThreadHelper.DoWhileLocked(
                cache,
                () => cache.TrySet(KEY, 0, duration: CacheConstants.VeryLongDuration, timeout: CacheConstants.VeryShortDuration),
                CacheConstants.TimingDuration);
            Assert.IsFalse(setResult);
        }

        [TestMethod]
        public void VerifyThatGetCanTimeout()
        {
            using var cache = new ThreadSafeCache();
            const string KEY = "Sjö"; cache.TrySet(KEY, 1);
            var getResult = ThreadHelper.DoWhileLocked(
                cache,
                () => cache.TryGet<int>(KEY, out var _, timeout: CacheConstants.VeryShortDuration),
                CacheConstants.TimingDuration);
            Assert.IsFalse(getResult);
        }

        [TestMethod]
        public void VerifyThatRemoveCanTimeout()
        {
            using var cache = new ThreadSafeCache();
            const string KEY = "Lake"; cache.TrySet(KEY, 1);
            var getResult = ThreadHelper.DoWhileLocked(
                cache,
                () => cache.TryRemove([KEY], timeout: CacheConstants.VeryShortDuration),
                CacheConstants.TimingDuration);
            Assert.IsFalse(getResult);
        }

        [TestMethod]
        public void VerifyThatUpdatingSharedCacheCanTimeout()
        {
            using var cache = ThreadSafeCache.Factory.GetOrCreateShared(nameof(VerifyThatUpdatingSharedCacheCanTimeout));
            const string KEY = "Sjö"; cache.TrySet(KEY, 1);
            using var tempFile = new TempFile();
            _ = cache.TrySaveToFile(tempFile.Info);
            Assert.ThrowsException<UpdateFromFileTimeoutException>(
                () =>
                {
                    var loadedCache = ThreadHelper.DoWhileLocked(
                        cache,
                        () => ThreadSafeCache.Factory.UpdateOrLoadSharedFromFile(tempFile.Info, timeout: CacheConstants.VeryShortDuration),
                        CacheConstants.TimingDuration);
                });
        }
    }
}
