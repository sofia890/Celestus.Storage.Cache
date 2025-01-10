using Celestus.Storage.Cache.PerformanceTest;
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
            const string KEY_1 = "serial";
            const int VALUE_1 = 57;
            _ = _cache.TrySet(KEY_1, VALUE_1);

            const string KEY_2 = "Mattis";
            const double VALUE_2 = 11.5679;
            _ = _cache.TrySet(KEY_2, VALUE_2);

            const string KEY_3 = "Lakris";
            DateTime VALUE_3 = DateTime.Now;
            _ = _cache.TrySet(KEY_3, VALUE_3);

            const string KEY_4 = "Ludde";
            ExampleRecord VALUE_4 = new(-9634, "VerifyThatSerializationWorks", 10000000M);
            _ = _cache.TrySet(KEY_4, VALUE_4);

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

            Assert.AreEqual(otherCache.TryGet<int>(KEY_1), (true, VALUE_1));
            Assert.AreEqual(otherCache.TryGet<double>(KEY_2), (true, VALUE_2));
            Assert.AreEqual(otherCache.TryGet<DateTime>(KEY_3), (true, VALUE_3));
            Assert.AreEqual(otherCache.TryGet<ExampleRecord>(KEY_4), (true, VALUE_4));
        }

        [TestMethod]
        public void VerifyThatItemsInCacheCanExpire()
        {
            //
            // Arrange
            //
            const int DURATION = 100;
            const int VALUE = 55;
            const string KEY = "key";
            _ = _cache.TrySet(KEY, VALUE, duration: TimeSpan.FromMilliseconds(DURATION));

            //
            // Act
            //
            Thread.Sleep(DURATION * 2);

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

        [TestMethod]
        public void VerifyThatSetCanTimeout()
        {
            //
            // Arrange
            //
            const int N_ITERATION = 1000;
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
            const int N_THREADS = 64;
            const int TIMEOUT = 1;

            const string KEY = "gunnar";
            _cache.TrySet(KEY, 1);

            Func<bool> ThreadWorkerBuilder(int id)
            {
                return () =>
                {
                    var succeeded = true;

                    for (int i = 1; i <= N_ITERATION; i++)
                    {
                        _ = _cache.TrySet(KEY, 0, timeout: TIMEOUT);

                        var (latestResult, _) = _cache.TryGet<int>(KEY, timeout: TIMEOUT);
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
