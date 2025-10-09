namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadSafeCacheEquitable
{
    [TestMethod]
    public void VerifyThatThreadSafeCacheWhenComparedToNullReturnsFalse()
    {
        using var cache = new ThreadSafeCache();
        Assert.IsFalse(cache.Equals(null));
    }
}
