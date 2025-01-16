namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadCacheEquatable
{
    [TestMethod]
    public void VerifyThatThreadCacheWhenComparedToNullReturnsFalse()
    {
        var cache = new ThreadCache();
        Assert.IsFalse(cache.Equals(null));
    }
}
