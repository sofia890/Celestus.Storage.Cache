using Celestus.Storage.Cache.Attributes;
using Celestus.Storage.Cache.Test.Model.ExtraNamespaceToCheckNested;
using System.Diagnostics;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public sealed class TestCacheAttribute
    {
        [TestMethod]
        public void TestThatPropertiesAreSetCorrectly()
        {
            //
            // Arrange
            //
            const int TIMEOUT_IN_MS = 26897;
            const int DURATION_IN_MS = 5766879;
            const string KEY = "keyTest";
            var cacheAttribute = new CacheAttribute(TIMEOUT_IN_MS, DURATION_IN_MS, KEY);

            //
            // Act & Assert
            //
            Assert.AreEqual(TIMEOUT_IN_MS, cacheAttribute.Timeout);
            Assert.AreEqual(DURATION_IN_MS, cacheAttribute.Duration);
            Assert.AreEqual(KEY, cacheAttribute.Key);
        }

        [TestMethod]
        [DoNotParallelize] // Timing tests become unreliable when run in parallel.
        public void VerifyThatCachingResultsSaveTime()
        {
            //
            // Arrange
            //
            SimpleClass simpleClass = new();

            //
            // Act
            //
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _ = simpleClass.SleepBeforeCalculationCached((1, 2), out _);
            _ = simpleClass.SleepBeforeCalculationCached((1, 2), out _);
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
            SimpleClass simpleClass = new();
            var other = new SimpleClass();

            //
            // Act
            //
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _ = simpleClass.SleepBeforeCalculationCached((1, 2), out _);
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
            SimpleClass simpleClass = new();

            //
            // Act
            //
            const int A = 30;
            const int B = 15;
            var returned = simpleClass.CalculateCached((A, B), out var c);

            //
            // Assert
            //
            const int EXPECTED_RETURN = 45;
            const int EXPECTED_C = 15;
            Assert.AreEqual(EXPECTED_RETURN, returned);
            Assert.AreEqual(EXPECTED_C, c);
        }

        [TestMethod]
        public void VerifyThatCachedDataIsCorrectOnFollowUpHit()
        {
            //
            // Arrange
            //
            SimpleClass simpleClass = new();

            //
            // Act
            //
            const int A = 30;
            const int B = 15;
            _ = simpleClass.CalculateCached((A, B), out var _);
            var returned2 = simpleClass.CalculateCached((A, B), out var c2);

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
            SimpleClass simpleClass = new();

            //
            // Act
            //
            const int A1 = 30;
            const int B1 = 15;
            var returned1 = simpleClass.CalculateCached((A1, B1), out var c1);

            const int A2 = 45;
            const int B2 = 5;
            var returned2 = simpleClass.CalculateCached((A2, B2), out var c2);

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