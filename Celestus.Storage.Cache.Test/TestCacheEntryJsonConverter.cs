using Celestus.Serialization;
using System.Text.Json;
using static Celestus.Storage.Cache.CacheEntry;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestCacheEntryJsonConverter
    {
        [TestMethod]
        public void VerifyThatWrongBaseTypeCausesCrash()
        {
            var json = "[]";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatMissingTypeCausesCrash()
        {
            var json = "{\"Expiration\":1234567890,\"Data\":\"some data\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatMissingDataCausesCrash()
        {
            var json = "{\"Expiration\":1234567890,\"Type\":\"Invalid.Type\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatMissingExpirationCausesCrash()
        {
            var json = "{\"Type\":\"System.String\",\"Data\":\"some data\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatInvalidTypeCausesCrash()
        {
            var json = "{\"Type\":\"Invalid.Type\",\"Expiration\":1234567890,\"Data\":\"some data\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatDataBeforeTypeCausesCrash()
        {
            var json = "{\"Data\":\"some data\",\"Type\":\"System.String\",\"Expiration\":1234567890}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatUnexpectedPropertyNameCausesCrash()
        {
            var json = "{\"JustAnotherProperty\":\"some data\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatNullCanBeSerialized()
        {
            //
            // Arrange
            //
            CacheEntry entry = new(0, null);

            //
            // Act
            //
            var path = new Uri(Path.GetTempFileName());
            Serialize.SaveToFile(entry, path);

            var json = File.ReadAllText(path.AbsolutePath);

            File.Delete(path.AbsolutePath);

            //
            // Assert
            //
            Assert.AreEqual("{\"Type\":null,\"Expiration\":0,\"Data\":null}", json);
        }

        [TestMethod]
        public void VerifyThatNullCanBeDeserialized()
        {
            var json = "{\"Type\":null,\"Expiration\":0,\"Data\":null}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }
    }
}

