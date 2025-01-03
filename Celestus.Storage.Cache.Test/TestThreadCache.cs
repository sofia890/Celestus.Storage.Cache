namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not threadsafe since they dispose of resource other tests use.
    public sealed class TestThreadCache
    {
        const int DEFAULT_TIMEOUT = 1000;

        private ThreadCache _cache = null!;

        [TestInitialize]
        public void Initialize()
        {
            _cache = ThreadCache.CreateShared(nameof(TestThreadCache), DEFAULT_TIMEOUT);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (!_cache.IsDisposed)
            {
                _cache.Dispose();
            }
        }

        [TestMethod]
        public void VerifyThatGetRetrievesSetValue()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            const int VALUE = 55;
            const string key = "key";

            _ = _cache.TrySet(key, VALUE);

            //
            // Assert
            //
            Assert.IsTrue(_cache.TryGet<int>(key) is (true, VALUE));
        }

        [TestMethod]
        public void VerifyThatSettingValueMultipleTimesWorks()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            const int VALUE = 55;
            const string KEY = "test";

            _ = _cache.TrySet(KEY, VALUE);
            _ = _cache.TrySet(KEY, VALUE + 1);
            _ = _cache.TrySet(KEY, VALUE + 2);
            _ = _cache.TrySet(KEY, VALUE + 3);
            _ = _cache.TrySet(KEY, VALUE + 4);

            //
            // Assert
            //
            Assert.IsTrue(_cache.TryGet<int>(KEY) is (true, VALUE + 4));
        }

        [TestMethod]
        public void VerifyThatCachesWithDifferentKeysAreUnique()
        {
            //
            // Arrange
            //
            var otherCache = ThreadCache.CreateShared("other", DEFAULT_TIMEOUT);

            const int VALUE = 55;
            const string KEY = "test";
            _ = _cache.TrySet(KEY, VALUE);

            //
            // Act
            //
            _ = otherCache.TrySet(KEY, VALUE + 1);
            var otherResult = otherCache.TryGet<int>(KEY);
            otherCache.Dispose();

            //
            // Assert
            //
            Assert.IsTrue(_cache.TryGet<int>(KEY) is (true, VALUE));
            Assert.IsTrue(otherResult is (true, VALUE + 1));
        }

        [TestMethod]
        public void VerifyThatNewCacheIsCreatedAfterFirstIsDisposed()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            _cache.Dispose();
            _cache = ThreadCache.CreateShared(nameof(TestThreadCache), DEFAULT_TIMEOUT);

            //
            // Assert
            //
            Assert.IsFalse(_cache.IsDisposed);
        }

        [TestMethod]
        public void VerifyThatCacheCanBeDisposed()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            _cache.Dispose();

            //
            // Assert
            //
            Assert.IsTrue(_cache.IsDisposed);
        }

        [TestMethod]
        public void VerifyThatEightThreadsCanUseCacheInParallelWithDifferentKeys()
        {
            //
            // Arrange
            //
            const int N_ITERATION = 1000;
            const int N_THREADS = 80;

            Action ThreadWorkerBuilder(int id)
            {
                return () =>
                {
                    var key = id.ToString();
                    _ = _cache.TrySet(key, id);

                    for (int i = 1; i <= N_ITERATION; i++)
                    {
                        var (result, value) = _cache.TryGet<int>(key);
                        _ = _cache.TrySet(key, value + 1);
                    }
                };
            }

            var threads = Enumerable.Range(0, N_THREADS)
                                    .Select(x => new Task(ThreadWorkerBuilder(x)))
                                    .ToArray();

            //
            // Act
            //
            foreach (var thread in threads)
            {
                thread.Start();
            }

            const int THREAD_TEST_TIMEOUT = 10000;
            _ = Task.WaitAll(threads, THREAD_TEST_TIMEOUT);

            //
            // Assert
            //
            for (int i = 0; i < N_THREADS; i++)
            {
                var key = i.ToString();
                Assert.AreEqual(N_ITERATION + i, _cache.TryGet<int>(key).data);
            }
        }

        [TestMethod]
        public void VerifyThatMultipleThreadsCanInteractWithSameKeyWithoutCrashing()
        {
            //
            // Arrange
            //
            const int N_ITERATION = 1000;
            const int N_THREADS = 80;

            var key = "hammer";

            Action ThreadWorkerBuilder(int id)
            {
                return () =>
                {
                    _ = _cache.TrySet(key, id);

                    for (int i = 1; i <= N_ITERATION; i++)
                    {
                        var (result, value) = _cache.TryGet<int>(key);
                        _ = _cache.TrySet(key, value * (i + 1));
                    }
                };
            }

            var threads = Enumerable.Range(0, N_THREADS)
                                    .Select(x => new Task(ThreadWorkerBuilder(x)))
                                    .ToArray();

            //
            // Act
            //
            foreach (var thread in threads)
            {
                thread.Start();
            }

            //
            // Assert
            //

            /* We do not care about anything but that nothing crashes.
             * There is no way to assert on the final value due to race condtions
             * between threads. */

            const int THREAD_TEST_TIMEOUT = 10000;
            _ = Task.WaitAll(threads, THREAD_TEST_TIMEOUT);
        }
    }
}
