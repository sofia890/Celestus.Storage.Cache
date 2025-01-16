namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    [DoNotParallelize] // The tests are not thread safe since they dispose of resource other tests use.
    public sealed class TestCacheSharedCreation
    {
        [TestMethod]
        public void VerifyThatSharedCacheCreatesKeyWhenNotProvided()
        {
            //
            // Arrange
            //

            //
            // Act
            //
            var cache = Cache.GetOrCreateShared();

            //
            // Assert
            //
            Assert.AreNotEqual(default, cache.Key);
        }
    }
}
