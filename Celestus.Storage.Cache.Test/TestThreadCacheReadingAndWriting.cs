using Celestus.Storage.Cache.Test.Model;

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

            var (result, _) = cache.TryGet<int>(KEY);

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
            var (resultPointA, _) = cache.TryGet<int>(KEY);

            ThreadHelper.SpinWait(interval / 2);

            var (resultPointB, _) = cache.TryGet<int>(KEY);

            ThreadHelper.SpinWait(interval);

            var (resultPointC, _) = cache.TryGet<int>(KEY);

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
            Assert.AreEqual((true, VALUE), cache.TryGet<int>(KEY));
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
            Assert.AreEqual((true, VALUE), cache.TryGet<string?>(KEY));
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
            Assert.AreEqual((true, VALUE + 4), cache.TryGet<int>(KEY));
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
                        var (result, value) = cache.TryGet<int>(key, timeout: CacheConstants.TimingDuration);

                        Assert.IsTrue(result);

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
                Assert.AreEqual(N_ITERATION + i, cache.TryGet<int>(key).data);
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
                        var (result, value) = cache.TryGet<int>(KEY, timeout: CacheConstants.TimingDuration);

                        Assert.IsTrue(result);

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
            var (result, data) = cache.TryGet<int>(KEY);
            Assert.IsTrue(result);
            Assert.IsTrue(data > 0);
        }
    }
}
