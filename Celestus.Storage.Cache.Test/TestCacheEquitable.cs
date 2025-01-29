namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestCacheEquitable
{
    [TestMethod]
    public void VerifyThatThreadCacheWhenComparedToNullReturnsFalse()
    {
        var cache = new Cache();
        Assert.IsFalse(cache.Equals(null));
    }
}
