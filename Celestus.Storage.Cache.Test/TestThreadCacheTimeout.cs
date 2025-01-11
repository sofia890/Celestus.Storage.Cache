namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not threadsafe since they dispose of resource other tests use.
    public sealed class TestThreadCacheTimeout
    {
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
            const int N_ITERATION = 100;
            const int N_THREADS = 32;
            const int TIMEOUT = 1;

            const string KEY = "spike";

            Func<bool> ThreadWorkerBuilder(int id)
            {
                return () =>
                {
                    var succeeded = true;

                    for (int i = 1; i <= N_ITERATION; i++)
                    {
                        succeeded &= _cache.TrySet(KEY, 0, timeout: TIMEOUT);
                    }

                    return succeeded;
                };
            }

            //
            // Act
            //
            var threads = Enumerable.Range(0, N_THREADS)
                                    .Select(x => Task.Run(ThreadWorkerBuilder(x)))
                                    .ToArray();

            //
            // Assert
            //
            const int THREAD_TEST_TIMEOUT = 10000;
            Assert.IsTrue(Task.WaitAll(threads, THREAD_TEST_TIMEOUT));
            Assert.IsFalse(threads.All(x => x.Result));
        }

        [TestMethod]
        public void VerifyThatGetCanTimeout()
        {
            //
            // Arrange
            //
            var longKey = TimeoutHelper.GetKeyThatTakesLongToHash();

            const string KEY = "Sjö";
            _cache.TrySet(KEY, 1);

            //
            // Act
            //
            var (threadsSucceeded, primaryResult) = TimeoutHelper.RunInParallel(
                primary: (
                    nrOfThreads: 16,
                    () => KEY,
                    (key, _) =>
                    {
                        var (latestResult, _) = _cache.TryGet<int>(key, timeout: 1);
                        return latestResult;
                    }
            ),

                secondary: (
                    nrOfThreads: 8,
                    () => longKey,
                    (key, _) => _cache.TrySet(key, 0, timeout: 10000)),

                iterationsPerThread: 1000,
                timeoutInMs: 10000);

            //
            // Assert
            //
            Assert.IsTrue(threadsSucceeded);
            Assert.IsTrue(primaryResult);
        }

        [TestMethod]
        public void VerifyThatUpdatingSharedCacheCanTimeout()
        {
            //
            // Arrange
            //
            var longKey = TimeoutHelper.GetKeyThatTakesLongToHash();

            const string KEY = "Sjö";
            _cache.TrySet(KEY, 1);

            //
            // Act
            //
            var (threadsSucceeded, primaryResult) = TimeoutHelper.RunInParallel(
                primary: (
                    nrOfThreads: 16,
                    () =>
                    {
                        var path = new Uri(Path.GetTempFileName());
                        _cache.SaveToFile(path);

                        return path;
                    },
                    (path, _) => ThreadCache.UpdateOrLoadSharedFromFile(path, timeout: 1) == null),

                secondary: (
                    nrOfThreads: 8,
                    () => longKey,
                    (key, _) => _cache.TrySet(key, 0, timeout: 10000)),

                iterationsPerThread: 1000,
                timeoutInMs: 10000);

            //
            // Assert
            //
            Assert.IsTrue(threadsSucceeded);
            Assert.IsTrue(primaryResult);
        }

        [TestMethod]
        public void VerifyThatUpdatingCacheCanTimeout()
        {
            //
            // Arrange
            //
            var longKey = TimeoutHelper.GetKeyThatTakesLongToHash();

            const string KEY = "Ön";
            _cache.TrySet(KEY, 1);

            //
            // Act
            //
            var (threadsSucceeded, primaryResult) = TimeoutHelper.RunInParallel(
                primary: (
                    nrOfThreads: 16,
                    () =>
                    {
                        var path = new Uri(Path.GetTempFileName());
                        _cache.SaveToFile(path);

                        return path;
                    },
                    (path, _) => ThreadCache.UpdateOrLoadSharedFromFile(path, timeout: 1) == null),

                secondary: (
                    nrOfThreads: 8,
                    () => longKey,
                    (key, _) => _cache.TrySet(key, 0, timeout: 1000)),

                iterationsPerThread: 1000,
                timeoutInMs: 10000);

            //
            // Assert
            //
            Assert.IsTrue(threadsSucceeded);
            Assert.IsTrue(primaryResult);
        }
    }
}
