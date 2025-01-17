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
            const int N_ITERATION = 100;
            const int N_THREADS = 16;

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
            const int N_ITERATION = 100;
            const int N_THREADS = 16;

            const string KEY = "hammer";

            Action ThreadWorkerBuilder(int id)
            {
                return () =>
                {
                    _ = _cache.TrySet(KEY, id);

                    for (int i = 1; i <= N_ITERATION; i++)
                    {
                        var (result, value) = _cache.TryGet<int>(KEY);
                        _ = _cache.TrySet(KEY, value * (i + 1));
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
             * There is no way to assert on the final value due to race conditions
             * between threads. */

            const int THREAD_TEST_TIMEOUT = 10000;
            _ = Task.WaitAll(threads, THREAD_TEST_TIMEOUT);
        }
    }
}
