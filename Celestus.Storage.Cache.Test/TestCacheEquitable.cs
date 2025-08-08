namespace Celestus.Storage.Cache.Test;

[TestClass]
public class TestCacheEquitable
{
    [TestMethod]
    public void VerifyThatThreadCacheWhenComparedToNullReturnsFalse()
    {
        //
        // Arrange & Act
        //
        using var cache = new Cache();
        
        //
        // Assert
        //
        Assert.IsFalse(cache.Equals(null));
    }
}
