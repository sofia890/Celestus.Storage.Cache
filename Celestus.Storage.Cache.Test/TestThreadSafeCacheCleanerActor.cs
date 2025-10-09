using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestThreadSafeCacheCleanerActor
    {
        [TestMethod]
        public void VerifyThatThreadSafeCacheCleanerActorDoesNotCrashWhenNoCacheIsSet()
        {
            var actor = new ThreadSafeCacheCleanerActor<string, string>(CacheConstants.ShortDuration);
            ThreadHelper.SpinWait(CacheConstants.LongDuration);
            actor.Dispose();
        }
    }
}
