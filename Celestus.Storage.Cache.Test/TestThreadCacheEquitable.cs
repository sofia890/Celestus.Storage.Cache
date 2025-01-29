namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadCacheEquitable
{
    [TestMethod]
    public void VerifyThatThreadCacheWhenComparedToNullReturnsFalse()
    {
        var cache = new ThreadCache();
        Assert.IsFalse(cache.Equals(null));
    }
}
