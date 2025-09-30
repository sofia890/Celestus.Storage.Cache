namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestCacheEntryEquitable
    {
        [TestMethod]
        public void VerifyThatCacheEntryEqualsWorksCorrectly()
        {
            //
            // Arrange
            //
            var entry1 = new CacheEntry(12345, "test value");
            var entry2 = new CacheEntry(12345, "test value");
            var entry3 = new CacheEntry(54321, "test value");
            var entry4 = new CacheEntry(12345, "different value");

            //
            // Act & Assert
            //
            Assert.AreEqual(entry1, entry2);
            Assert.AreNotEqual(entry1, entry3);
            Assert.AreNotEqual(entry1, entry4);
            Assert.AreNotEqual(entry1, null);
        }

        [TestMethod]
        public void VerifyThatCacheEntryHashCodeIsConsistent()
        {
            //
            // Arrange
            //
            var complexObject = new Dictionary<string, object>
            {
                { "int", 42 },
                { "string", "test" },
                { "list", new List<int> { 1, 2, 3 } }
            };

            var entry1 = new CacheEntry(12345, complexObject);
            var entry2 = new CacheEntry(12345, complexObject);

            //
            // Act & Assert
            //
            Assert.AreEqual(entry1.GetHashCode(), entry2.GetHashCode());
        }
    }
}