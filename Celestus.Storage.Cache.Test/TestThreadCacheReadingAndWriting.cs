using Celestus.Storage.Cache.Test.Model;
using Newtonsoft.Json.Linq;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public sealed class TestThreadCacheReadingAndWriting
    {
        [TestMethod]
        public void VerifyThatItemsInCacheCanExpire()
        {
            //
            // Arrange
            //
            using var cache = new ThreadCache();

            const int DURATION_IN_MS = 10;
            const int VALUE = 55;
            const string KEY = "key";
            _ = cache.TrySet(KEY, VALUE, duration: TimeSpan.FromMilliseconds(DURATION_IN_MS));

            //
            // Act
            //
            ThreadHelper.SpinWait(DURATION_IN_MS * 2);

            var result = cache.TryGet<int>(KEY, out _);

            //
            // Assert
            //
            Assert.IsFalse(result);
        }

        [TestMethod]
        [DoNotParallelize] // Timing tests are not reliable when run in parallel.
        public void VerifyThatItemsInCacheCanBeAccessedBeforeExpiration()
        {
            //
            // Arrange
            //
            var interval = CacheConstants.TimingDuration;
            using var cache = new ThreadCache(interval);

            const int VALUE = 23;
            const string KEY = "key";
            _ = cache.TrySet(KEY, VALUE, duration: interval);

            //
            // Act
            //
            var resultPointA = cache.TryGet<int>(KEY, out _);

            ThreadHelper.SpinWait(interval / 2);

            var resultPointB = cache.TryGet<int>(KEY, out _);

            ThreadHelper.SpinWait(interval);

            var resultPointC = cache.TryGet<int>(KEY, out _);

            //
            // Assert
            //
            Assert.IsTrue(resultPointA);
            Assert.IsTrue(resultPointB);
            Assert.IsFalse(resultPointC);
        }

        [TestMethod]
        public void VerifyThatGetRetrievesSetValue()
        {
            //
            // Arrange
            //
            using var cache = new ThreadCache();

            //
            // Act
            //
            const int VALUE = 55;
            const string KEY = "key";
            _ = cache.TrySet(KEY, VALUE);

            //
            // Assert
            //
            Assert.IsTrue(cache.TryGet<int>(KEY, out var value));
            Assert.AreEqual(VALUE, value);
        }

        [TestMethod]
        public void VerifyThatCacheCanHoldNullValues()
        {
            //
            // Arrange
            //
            using var cache = new ThreadCache();

            //
            // Act
            //
            const string? VALUE = null;
            const string KEY = "key";
            _ = cache.TrySet(KEY, VALUE);

            //
            // Assert
            //
            Assert.IsTrue(cache.TryGet<string?>(KEY, out var value));
            Assert.AreEqual(VALUE, value);
        }

        [TestMethod]
        public void VerifyThatSettingValueMultipleTimesWorks()
        {
            //
            // Arrange
            //
            using var cache = new ThreadCache();

            //
            // Act
            //
            const int VALUE = 55;
            const string KEY = "test";

            _ = cache.TrySet(KEY, VALUE);
            _ = cache.TrySet(KEY, VALUE + 1);
            _ = cache.TrySet(KEY, VALUE + 2);
            _ = cache.TrySet(KEY, VALUE + 3);
            _ = cache.TrySet(KEY, VALUE + 4);

            //
            // Assert
            //
            Assert.IsTrue(cache.TryGet<int>(KEY, out var value));
            Assert.AreEqual(VALUE + 4, value);
        }

        [TestMethod]
        public void VerifyThatEightThreadsCanUseCacheInParallelWithDifferentKeys()
        {
            //
            // Arrange
            //
            using var cache = new ThreadCache();

            ManualResetEvent startSignal = new(false);

            const int N_ITERATION = 100;
            const int N_THREADS = 16;

            Action ThreadWorkerBuilder(int id)
            {
                return () =>
                {
                    startSignal.WaitOne();

                    var key = id.ToString();

                    Assert.IsTrue(cache.TrySet(key, id, timeout: CacheConstants.TimingDuration));

                    for (int i = 1; i <= N_ITERATION; i++)
                    {
                        Assert.IsTrue(cache.TryGet<int>(key, out var value, timeout: CacheConstants.TimingDuration));

                        Assert.IsTrue(cache.TrySet(key, value + 1, timeout: CacheConstants.TimingDuration));
                    }
                };
            }

            var threads = Enumerable.Range(0, N_THREADS)
                                    .Select(x => Task.Run(ThreadWorkerBuilder(x)))
                                    .ToArray();

            //
            // Act
            //
            startSignal.Set();
            _ = Task.WaitAll(threads, CacheConstants.VeryLongDuration);

            //
            // Assert
            //
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
            //
            // Arrange
            //
            using var cache = new ThreadCache();

            const string KEY = "hammer";
            Assert.IsTrue(cache.TrySet(KEY, 0, timeout: CacheConstants.TimingDuration));

            ManualResetEvent startSignal = new(false);

            const int N_ITERATION = 100;
            const int N_THREADS = 16;

            Action ThreadWorkerBuilder(int id)
            {
                return () =>
                {
                    startSignal.WaitOne();

                    for (int i = 1; i <= N_ITERATION; i++)
                    {
                        Assert.IsTrue(cache.TryGet<int>(KEY, out var value, timeout: CacheConstants.TimingDuration));

                        Assert.IsTrue(cache.TrySet(KEY, value + 1, timeout: CacheConstants.TimingDuration));
                    }
                };
            }

            var threads = Enumerable.Range(0, N_THREADS)
                                    .Select(x => Task.Run(ThreadWorkerBuilder(x)))
                                    .ToArray();

            //
            // Act
            //
            startSignal.Set();
            _ = Task.WaitAll(threads, CacheConstants.VeryLongDuration);

            //
            // Assert
            //
            Assert.IsTrue(cache.TryGet<int>(KEY, out var data));
            Assert.IsTrue(data > 0);
        }
    }
}
