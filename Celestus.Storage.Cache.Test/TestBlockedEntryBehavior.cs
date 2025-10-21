using Celestus.Storage.Cache.Test.Model;
using System.Text.Json;

namespace Celestus.Storage.Cache.Test;

[TestClass]
[DoNotParallelize]
public class TestBlockedEntryBehavior
{
    /// <summary>
    /// Helper to create serializer options with a configured register.
    /// A fresh register is used per test to ensure isolation.
    /// </summary>
    private static JsonSerializerOptions CreateOptions(CacheTypeFilterMode mode, BlockedEntryBehavior behavior, params Type[] registeredTypes)
    {
        var options = CacheEntryHelper.CreateOptions(behavior);
        var register = new CacheTypeRegister
        {
            Mode = mode
        };
        foreach (var t in registeredTypes)
        {
            register.Register(t);
        }
        options.SetCacheTypeRegister(register);
        return options;
    }

    [TestMethod]
    public void VerifyWhitelistBlocksUnregisteredTypeThrows()
    {
        //
        // Arrange
        //
        const string value = "abc";
        var json = CacheEntryHelper.Serialize(value);
        var options = CreateOptions(CacheTypeFilterMode.Whitelist, BlockedEntryBehavior.Throw /* no registrations */);

        //
        // Act & Assert
        //
        Assert.ThrowsException<BlockedCacheTypeException>(() => JsonSerializer.Deserialize<CacheEntry?>(json, options));
    }

    [TestMethod]
    public void VerifyWhitelistAllowsRegisteredType()
    {
        //
        // Arrange
        //
        const string value = "data";
        var json = CacheEntryHelper.Serialize(value);
        var options = CreateOptions(CacheTypeFilterMode.Whitelist, BlockedEntryBehavior.Throw, typeof(string));

        //
        // Act
        //
        var entry = JsonSerializer.Deserialize<CacheEntry>(json, options);

        //
        // Assert
        //
        Assert.AreEqual(value, entry?.Data as string);
    }

    [TestMethod]
    public void VerifyWhitelistIgnoreUnregisteredTypeReturnsNull()
    {
        //
        // Arrange
        //
        const string value = "ignored";
        var json = CacheEntryHelper.Serialize(value);
        var options = CreateOptions(CacheTypeFilterMode.Whitelist, BlockedEntryBehavior.Ignore /* no registrations */);

        //
        // Act
        //
        var entry = JsonSerializer.Deserialize<CacheEntry?>(json, options);

        //
        // Assert
        //
        Assert.IsNull(entry);
    }

    [TestMethod]
    public void VerifyBlacklistBlocksRegisteredTypeThrows()
    {
        //
        // Arrange
        //
        const int value = 5;
        var json = CacheEntryHelper.Serialize(value);
        var options = CreateOptions(CacheTypeFilterMode.Blacklist, BlockedEntryBehavior.Throw, typeof(int));

        //
        // Act & Assert
        //
        Assert.ThrowsException<BlockedCacheTypeException>(() => JsonSerializer.Deserialize<CacheEntry?>(json, options));
    }

    [TestMethod]
    public void VerifyBlacklistBlocksRegisteredTypeIgnored()
    {
        //
        // Arrange
        //
        const int value = 42;
        var json = CacheEntryHelper.Serialize(value);
        var options = CreateOptions(CacheTypeFilterMode.Blacklist, BlockedEntryBehavior.Ignore, typeof(int));

        //
        // Act
        //
        var entry = JsonSerializer.Deserialize<CacheEntry?>(json, options);

        //
        // Assert
        //
        Assert.IsNull(entry);
    }

    [TestMethod]
    public void VerifyBlacklistAllowsUnregisteredType()
    {
        //
        // Arrange
        //
        const string value = "ok";
        var json = CacheEntryHelper.Serialize(value);
        var options = CreateOptions(CacheTypeFilterMode.Blacklist, BlockedEntryBehavior.Throw /* no registrations */);

        //
        // Act
        //
        var entry = JsonSerializer.Deserialize<CacheEntry>(json, options);

        //
        // Assert
        //
        Assert.AreEqual(value, entry?.Data as string);
    }

    [TestMethod]
    public void VerifyClearResetsRegisteredBlock()
    {
        //
        // Arrange
        //
        const int value = 7;
        var json = CacheEntryHelper.Serialize(value);
        var register = new CacheTypeRegister(); // default blacklist mode
        register.Register(typeof(int));
        register.Clear(); // remove registration so type should be allowed
        var options = CacheEntryHelper.CreateOptions(BlockedEntryBehavior.Throw);
        options.SetCacheTypeRegister(register);

        //
        // Act
        //
        var entry = JsonSerializer.Deserialize<CacheEntry>(json, options);

        //
        // Assert
        //
        Assert.AreEqual(value, (int)entry?.Data!);
    }

    [TestMethod]
    public void VerifyOptionsIsolationBetweenBehaviors()
    {
        //
        // Arrange
        //
        const int value = 11;
        var json = CacheEntryHelper.Serialize(value);
        var register = new CacheTypeRegister
        {
            Mode = CacheTypeFilterMode.Blacklist
        };
        register.Register(typeof(int));

        var ignoreOptions = CacheEntryHelper.CreateOptions(BlockedEntryBehavior.Ignore);
        ignoreOptions.SetCacheTypeRegister(register);

        var throwOptions = CacheEntryHelper.CreateOptions(BlockedEntryBehavior.Throw);
        throwOptions.SetCacheTypeRegister(register);

        //
        // Act
        //
        var ignored = JsonSerializer.Deserialize<CacheEntry?>(json, ignoreOptions);

        //
        // Assert
        //
        Assert.IsNull(ignored);
        Assert.ThrowsException<BlockedCacheTypeException>(() => JsonSerializer.Deserialize<CacheEntry?>(json, throwOptions));
    }
}
