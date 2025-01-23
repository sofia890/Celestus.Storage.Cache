namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not thread safe since they dispose of resource other tests use.
    public sealed class TestThreadCacheReadingAndWriting
    {
        private ThreadCache _cache = null!;

        [TestInitialize]
        public void Initialize()
        {
            _cache = new();
        }

        [TestMethod]
        public void VerifyThatItemsInCacheCanExpire()
        {
            //
            // Arrange
            //
            const int DURATION = 10;
            const int VALUE = 55;
            const string KEY = "key";
            _ = _cache.TrySet(KEY, VALUE, duration: TimeSpan.FromMilliseconds(DURATION));

            //
            // Act
            //
            System.Threading.Thread.Sleep(DURATION * 2);

            var (result, _) = _cache.TryGet<int>(KEY);

            //
            // Assert
            //
            Assert.IsFalse(result);
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
            const string KEY = "key";
            _ = _cache.TrySet(KEY, VALUE);

            //
            // Assert
            //
            Assert.IsTrue(_cache.TryGet<int>(KEY) is (true, VALUE));
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
        public void VerifyThatEightThreadsCanUseCacheInParallelWithDifferentKeys()
        {
            //
            // Arrange
            //
            ManualResetEvent startSignal = new(false);

            const int N_ITERATION = 100;
            const int N_THREADS = 16;
            const int THREAD_TEST_TIMEOUT = 10000;

            Action ThreadWorkerBuilder(int id)
            {
                return () =>
                {
                    startSignal.WaitOne();

                    var key = id.ToString();
                    _ = _cache.TrySet(key, id, timeout: THREAD_TEST_TIMEOUT);

                    for (int i = 1; i <= N_ITERATION; i++)
                    {
                        var (result, value) = _cache.TryGet<int>(key, THREAD_TEST_TIMEOUT);
                        _ = _cache.TrySet(key, value + 1);
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
            const string KEY = "hammer";
            const int THREAD_TEST_TIMEOUT = 10000;
            _ = _cache.TrySet(KEY, 0, timeout: THREAD_TEST_TIMEOUT);

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
                        var (_, value) = _cache.TryGet<int>(KEY, THREAD_TEST_TIMEOUT);
                        _ = _cache.TrySet(KEY, value + 1);
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
            _ = Task.WaitAll(threads, THREAD_TEST_TIMEOUT);

            //
            // Assert
            //
            var value = _cache.TryGet<int>(KEY);
            Assert.IsTrue(value.result);
            Assert.IsTrue(value.data > 0);
        }
    }
}
