using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadSafeCache
{
    [TestMethod]
    public void VerifyThatTrySetWithZeroTimeoutFailsImmediatelyWhenLocked()
    {
        using var cache = new ThreadSafeCache();
        cache.TrySet("initial", "value");

        bool Act() => cache.TrySet("key", "value", timeout: CacheConstants.ZeroDuration);

        var result = ThreadHelper.DoWhileLocked(cache, Act, CacheConstants.TimingDuration);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyThatTryGetWithZeroTimeoutFailsImmediatelyWhenLocked()
    {
        using var cache = new ThreadSafeCache();
        cache.TrySet("key", "value");

        (bool, string?) Act()
        {
            var success = cache.TryGet<string>("key", out var value, timeout: CacheConstants.VeryShortDuration);
            return (success, value);
        }

        var result = ThreadHelper.DoWhileLocked(cache, Act, CacheConstants.TimingDuration);

        Assert.IsTrue(result is (false, _));
    }

    [TestMethod]
    public void VerifyThatTryRemoveWithZeroTimeoutFailsImmediatelyWhenLocked()
    {
        using var cache = new ThreadSafeCache();
        cache.TrySet("key", "value");

        bool Act() => cache.TryRemove(["key"], timeout: CacheConstants.VeryShortDuration);

        var result = ThreadHelper.DoWhileLocked(cache, Act, CacheConstants.TimingDuration);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyThatCacheLockReturnsFalseWhenNoLockTimesOut()
    {
        using var cache = new ThreadSafeCache();

        void Act() => Assert.IsFalse(cache.TryGetWriteLock(out _, timeout: CacheConstants.VeryShortDuration));

        ThreadHelper.DoWhileLocked(cache, Act, CacheConstants.TimingDuration);
    }
}
