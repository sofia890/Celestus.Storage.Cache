using Celestus.Storage.Cache.PerformanceTest.ExtraNamespaceToCheckNested;
using System.Diagnostics;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not threadsafe since they dispose of resource other tests use.
    public sealed class TestCacheAttribute
    {
        private SimpleClass _simpleClass = null!;

        [TestInitialize]
        public void Initialize()
        {
            _simpleClass = new SimpleClass();
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void VerifyThatCachingResultsSaveTime()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _ = _simpleClass.SleepBeforeCalculationCached((1, 2), out _);
            _ = _simpleClass.SleepBeforeCalculationCached((1, 2), out _);
            stopwatch.Stop();

            //
            // Assert
            //
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < SimpleClass.CALCULATE_WITTH_SLEEP_TIMEOUT);
        }

        [TestMethod]
        public void VerifyThatCacheDataIsIsolatedForInstances()
        {
            //
            // Arrange
            //
            var other = new SimpleClass();

            //
            // Act
            //
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _ = _simpleClass.SleepBeforeCalculationCached((1, 2), out _);
            _ = other.SleepBeforeCalculationCached((1, 2), out _);
            stopwatch.Stop();

            //
            // Assert
            //
            Assert.IsTrue(stopwatch.ElapsedMilliseconds >= SimpleClass.CALCULATION_SLEEP * 2);
        }

        [TestMethod]
        public void VerifyThatCachedDataIsCorrectOnFirstHit()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            const int A = 30;
            const int B = 15;
            var returned = _simpleClass.CalculateCached((A, B), out var c);

            //
            // Assert
            //
            const int EXPECTED_RETURN = 45;
            const int EXPECTED_C = 15;
            Assert.AreEqual(EXPECTED_RETURN, returned);
            Assert.AreEqual(EXPECTED_C, c);
        }

        [TestMethod]
        public void VerifyThatCachedDataIsCorrectOnFollowupHit()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            const int A = 30;
            const int B = 15;
            _ = _simpleClass.CalculateCached((A, B), out var _);
            var returned2 = _simpleClass.CalculateCached((A, B), out var c2);

            //
            // Assert
            //
            const int EXPECTED_RETURN = 45;
            const int EXPECTED_C = 15;
            Assert.AreEqual(EXPECTED_RETURN, returned2);
            Assert.AreEqual(EXPECTED_C, c2);
        }

        [TestMethod]
        public void VerifyThatStaticMethodsWork()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            const int A = 100;
            const int B = 73;
            var returned = SimpleClass.CalculateStaticCached((A, B), out var c);

            //
            // Assert
            //
            const int EXPECTED_RETURN = 173;
            const int EXPECTED_C = 27;
            Assert.AreEqual(EXPECTED_RETURN, returned);
            Assert.AreEqual(EXPECTED_C, c);
        }

        [TestMethod]
        public void VerifyThatDataIsCachedBasedOnParameterValues()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            const int A1 = 30;
            const int B1 = 15;
            var returned1 = _simpleClass.CalculateCached((A1, B1), out var c1);

            const int A2 = 45;
            const int B2 = 5;
            var returned2 = _simpleClass.CalculateCached((A2, B2), out var c2);

            //
            // Assert
            //
            const int EXPECTED_RETURN_1 = 45;
            const int EXPECTED_C_1 = 15;
            Assert.AreEqual(EXPECTED_RETURN_1, returned1);
            Assert.AreEqual(EXPECTED_C_1, c1);

            const int EXPECTED_RETURN_2 = 50;
            const int EXPECTED_C_2 = 40;
            Assert.AreEqual(EXPECTED_RETURN_2, returned2);
            Assert.AreEqual(EXPECTED_C_2, c2);
        }
    }
}