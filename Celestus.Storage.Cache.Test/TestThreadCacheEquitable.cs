namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestThreadCacheEquitable
{
    [TestMethod]
    public void VerifyThatThreadCacheWhenComparedToNullReturnsFalse()
    {
        //
        // Arrange & Act
        //
        using var cache = new ThreadCache();

        //
        // Assert
        //
        Assert.IsFalse(cache.Equals(null));
    }
}
