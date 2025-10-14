using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public sealed class TestThreadSafeCacheReadingAndWriting
    {
        [TestMethod]
        public void VerifyThatItemsInCacheCanExpire()
        {
            using var cache = new ThreadSafeCache();
            const int DURATION_IN_MS = 10;
            const int VALUE = 55;
            const string KEY = "key";
            _ = cache.TrySet(KEY, VALUE, duration: TimeSpan.FromMilliseconds(DURATION_IN_MS));
            ThreadHelper.SpinWait(DURATION_IN_MS * 2);
            var result = cache.TryGet<int>(KEY, out _);
            Assert.IsFalse(result);
        }

        [TestMethod]
        [DoNotParallelize]
        public void VerifyThatItemsInCacheCanBeAccessedBeforeExpiration()
        {
            var duration = CacheConstants.TimingDuration;
            using var cache = new ThreadSafeCache(duration);
            const int VALUE = 23;
            const string KEY = "key";
            _ = cache.TrySet(KEY, VALUE, duration: duration);
            TimeSpan KEY_AVAILABLE_DURATION = duration / 2;
            const int NROF_CHECKS = 20;
            var keyExpiredEarly = ThreadHelper.DoPeriodicallyUntil(() => !cache.TryGet<int>(KEY, out _), NROF_CHECKS, KEY_AVAILABLE_DURATION / NROF_CHECKS, KEY_AVAILABLE_DURATION);
            var keyExpiredAfterDuration = ThreadHelper.DoPeriodicallyUntil(() => !cache.TryGet<int>(KEY, out _), CacheConstants.TimingIterations, CacheConstants.TimingIterationInterval, CacheConstants.VeryLongDuration);
            Assert.IsFalse(keyExpiredEarly);
            Assert.IsTrue(keyExpiredAfterDuration);
        }

        [TestMethod]
        public void VerifyThatGetRetrievesSetValue()
        {
            using var cache = new ThreadSafeCache();
            const int VALUE = 55;
            const string KEY = "key";
            _ = cache.TrySet(KEY, VALUE);
            Assert.IsTrue(cache.TryGet<int>(KEY, out var value));
            Assert.AreEqual(VALUE, value);
        }

        [TestMethod]
        public void VerifyThatCacheCanHoldNullValues()
        {
            using var cache = new ThreadSafeCache();
            const string? VALUE = null;
            const string KEY = "key";
            _ = cache.TrySet(KEY, VALUE);
            Assert.IsTrue(cache.TryGet<string?>(KEY, out var value));
            Assert.AreEqual(VALUE, value);
        }

        [TestMethod]
        public void VerifyThatSettingValueMultipleTimesWorks()
        {
            using var cache = new ThreadSafeCache();
            const int VALUE = 55;
            const string KEY = "test";
            _ = cache.TrySet(KEY, VALUE);
            _ = cache.TrySet(KEY, VALUE + 1);
            _ = cache.TrySet(KEY, VALUE + 2);
            _ = cache.TrySet(KEY, VALUE + 3);
            _ = cache.TrySet(KEY, VALUE + 4);
            Assert.IsTrue(cache.TryGet<int>(KEY, out var value));
            Assert.AreEqual(VALUE + 4, value);
        }

        [TestMethod]
        public void VerifyThatEightThreadsCanUseCacheInParallelWithDifferentKeys()
        {
            using var cache = new ThreadSafeCache();
            ManualResetEvent startSignal = new(false);
            const int N_ITERATION = 100;
            const int N_THREADS = 16;

            Action ThreadWorkerBuilder(int id) => () =>
            {
                startSignal.WaitOne();
                var key = id.ToString();
                Assert.IsTrue(cache.TrySet(key, id, duration: CacheConstants.VeryLongDuration, timeout: CacheConstants.TimingDuration));
                for (int i = 1; i <= N_ITERATION; i++)
                {
                    Assert.IsTrue(cache.TryGet<int>(key, out var value, timeout: CacheConstants.TimingDuration));
                    Assert.IsTrue(cache.TrySet(key, value + 1, duration: CacheConstants.VeryLongDuration, timeout: CacheConstants.TimingDuration));
                }
            };

            var threads = Enumerable.Range(0, N_THREADS).Select(x => Task.Run(ThreadWorkerBuilder(x))).ToArray();
            startSignal.Set();
            _ = Task.WaitAll(threads, CacheConstants.VeryLongDuration);
            for (int i = 0; i < N_THREADS; i++)
            {
                var key = i.ToString();
                Assert.IsTrue(cache.TryGet<int>(key, out var finalValue));
                Assert.AreEqual(N_ITERATION + i, finalValue);
            }
        }

        [TestMethod]
        public void VerifyThatMultipleThreadsCanInteractWithSameKeyWithoutCrashing()
        {
            using var cache = new ThreadSafeCache();
            const string KEY = "hammer";
            Assert.IsTrue(cache.TrySet(KEY, 0, duration: CacheConstants.VeryLongDuration, timeout: CacheConstants.TimingDuration));
            ManualResetEvent startSignal = new(false);
            const int N_ITERATION = 100;
            const int N_THREADS = 16;

            Action ThreadWorkerBuilder(int id) => () =>
            {
                startSignal.WaitOne();
                for (int i = 1; i <= N_ITERATION; i++)
                {
                    Assert.IsTrue(cache.TryGet<int>(KEY, out var value, timeout: CacheConstants.TimingDuration));
                    Assert.IsTrue(cache.TrySet(KEY, value + 1, duration: CacheConstants.VeryLongDuration, timeout: CacheConstants.TimingDuration));
                }
            };

            var threads = Enumerable.Range(0, N_THREADS).Select(x => Task.Run(ThreadWorkerBuilder(x))).ToArray();
            startSignal.Set();
            _ = Task.WaitAll(threads, CacheConstants.VeryLongDuration);
            Assert.IsTrue(cache.TryGet<int>(KEY, out var data));
            Assert.IsTrue(data > 0);
        }
    }
}
