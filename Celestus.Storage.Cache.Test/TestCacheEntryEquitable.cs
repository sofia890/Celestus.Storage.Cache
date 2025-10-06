namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestCacheEntryEquitable
    {
        [TestMethod]
        public void VerifyThatCacheEntryEqualsWorksCorrectly()
        {
            // Arrange
            var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var entry1 = new CacheEntry(baseTime, "test value");
            var entry2 = new CacheEntry(baseTime, "test value");
            var entry3 = new CacheEntry(baseTime.AddMinutes(1), "test value");
            var entry4 = new CacheEntry(baseTime, "different value");

            // Act & Assert
            Assert.AreEqual(entry1, entry2);
            Assert.AreNotEqual(entry1, entry3);
            Assert.AreNotEqual(entry1, entry4);
            Assert.AreNotEqual(entry1, null);
        }

        [TestMethod]
        public void VerifyThatCacheEntryHashCodeIsConsistent()
        {
            // Arrange
            var complexObject = new Dictionary<string, object>
            {
                { "int", 42 },
                { "string", "test" },
                { "list", new List<int> { 1, 2, 3 } }
            };

            var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var entry1 = new CacheEntry(baseTime, complexObject);
            var entry2 = new CacheEntry(baseTime, complexObject);

            // Act & Assert
            Assert.AreEqual(entry1.GetHashCode(), entry2.GetHashCode());
        }
    }
}