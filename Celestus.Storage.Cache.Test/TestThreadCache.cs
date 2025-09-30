using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadCache
{

    [TestMethod]
    public void VerifyThatTrySetWithZeroTimeoutFailsImmediatelyWhenLocked()
    {
        //
        // Arrange
        //
        using var cache = new ThreadCache();
        cache.TrySet("initial", "value");

        //
        // Act
        //
        bool Act()
        {
            return cache.TrySet("key", "value", timeout: CacheConstants.ZeroDuration);
        }

        var result = ThreadHelper.DoWhileLocked(cache, Act, CacheConstants.TimingDuration);

        //
        // Assert
        //
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyThatTryGetWithZeroTimeoutFailsImmediatelyWhenLocked()
    {
        //
        // Arrange
        //
        using var cache = new ThreadCache();
        cache.TrySet("key", "value");

        //
        // Act
        //
        (bool, string?) Act()
        {
            var success = cache.TryGet<string>("key", out var value, timeout: CacheConstants.VeryShortDuration);
            return (success, value);
        }

        var result = ThreadHelper.DoWhileLocked(cache, Act, CacheConstants.TimingDuration);

        //
        // Assert
        //
        Assert.IsTrue(result is (false, _));
    }

    [TestMethod]
    public void VerifyThatTryRemoveWithZeroTimeoutFailsImmediatelyWhenLocked()
    {
        //
        // Arrange
        //
        using var cache = new ThreadCache();
        cache.TrySet("key", "value");

        //
        // Act & Assert
        //
        bool Act()
        {
            return cache.TryRemove(["key"], timeout: CacheConstants.VeryShortDuration);
        }

        var result = ThreadHelper.DoWhileLocked(cache, Act, CacheConstants.TimingDuration);

        //
        // Assert
        //
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyThatCacheLockReturnsFalseWhenNoLockTimesOut()
    {
        //
        // Arrange
        //
        using var cache = new ThreadCache();

        //
        // Act & Assert
        //
        void Act()
        {
            Assert.IsFalse(cache.TryGetWriteLock(out _, timeout: CacheConstants.VeryShortDuration));
        }

        ThreadHelper.DoWhileLocked(cache, Act, CacheConstants.TimingDuration);
    }
}
