using Newtonsoft.Json;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not threadsafe since they dispose of resource other tests use.
    public sealed class TestThreadCache
    {
        private ThreadCache _cache = null!;

        [TestInitialize]
        public void Initialize()
        {
            _cache = ThreadCache.CreateShared(nameof(TestThreadCache));
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
        public void VerifyThatSerializationWorks()
        {
            //
            // Arrange
            //
            const string key = "serial";
            _ = _cache.TrySet(key, 1);


            //
            // Act
            //
            var serializer = JsonSerializer.Create();
            string serializedData;

            using (var stringWriter = new StringWriter())
            using (var writer = new JsonTextWriter(stringWriter))
            {
                serializer.Serialize(writer, _cache);
                serializedData = stringWriter.ToString();
            }

            ThreadCache? otherCache = null;

            using (var stringReader = new StringReader(serializedData))
            using (var reader = new JsonTextReader(stringReader))
            {
                otherCache = serializer.Deserialize(reader, _cache.GetType()) as ThreadCache;
            }

            //
            // Assert
            //
            Assert.IsNotNull(otherCache);
            Assert.AreEqual(_cache, otherCache);
        }

        [TestMethod]
        public void VerifyThatItemsInCacheCanExpire()
        {
            //
            // Arrange
            //
            const int DURATION = 100;
            const int VALUE = 55;
            const string key = "key";
            _ = _cache.TrySet(key, VALUE, duration: TimeSpan.FromMilliseconds(DURATION));

            //
            // Act
            //
            Thread.Sleep(DURATION * 2);

            var (result, _) = _cache.TryGet<int>(key);

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
            var otherCache = ThreadCache.CreateShared("other");

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
            _cache = ThreadCache.CreateShared(nameof(TestThreadCache));

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
            const int N_THREADS = 32;

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
            const int N_THREADS = 32;

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

        [TestMethod]
        public void VerifyThatSetCanTimeout()
        {
            //
            // Arrange
            //
            const int N_ITERATION = 1000;
            const int N_THREADS = 32;
            const int TIMEOUT = 1;

            var key = "hammer";

            Func<bool> ThreadWorkerBuilder(int id)
            {
                return () =>
                {
                    var succeeded = true;

                    for (int i = 1; i <= N_ITERATION; i++)
                    {
                        succeeded &= _cache.TrySet(key, 0, timeout: TIMEOUT);
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
            _ = Task.WaitAll(threads, THREAD_TEST_TIMEOUT);

            Assert.IsFalse(threads.All(x => x.Result));
        }

        [TestMethod]
        public void VerifyThatGetCanTimeout()
        {
            //
            // Arrange
            //
            const int N_ITERATION = 1000;
            const int N_THREADS = 32;
            const int TIMEOUT = 1;

            var key = "gunnar";
            _cache.TrySet(key, 1);

            Func<bool> ThreadWorkerBuilder(int id)
            {
                return () =>
                {
                    var succeeded = true;

                    for (int i = 1; i <= N_ITERATION; i++)
                    {
                        _ = _cache.TrySet(key, 0, timeout: TIMEOUT);

                        var (latestResult, _) = _cache.TryGet<int>(key, timeout: TIMEOUT);
                        succeeded &= latestResult;
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
            _ = Task.WaitAll(threads, THREAD_TEST_TIMEOUT);

            Assert.IsFalse(threads.All(x => x.Result));
        }
    }
}
