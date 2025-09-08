using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestThreadCacheCleanerActor
    {
        [TestMethod]
        public void VerifyThatThreadCacheCleanerActorDoesNotCrashWhenNoCacheIsSet()
        {
            //
            // Arrange
            //
            var actor = new ThreadCacheCleanerActor<string>(CacheConstants.ShortDuration);

            //
            // Act
            //
            ThreadHelper.SpinWait(CacheConstants.LongDuration);

            //
            // Assert
            //

            // No crash is a success.

            // Cleanup
            actor.Dispose();
        }
    }
}
