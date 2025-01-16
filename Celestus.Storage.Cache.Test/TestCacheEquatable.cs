namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestCacheEquatable
{
    [TestMethod]
    public void VerifyThatThreadCacheWhenComparedToNullReturnsFalse()
    {
        var cache = new Cache();
        Assert.IsFalse(cache.Equals(null));
    }
}
