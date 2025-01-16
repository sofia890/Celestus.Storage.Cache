using System.Text.Json;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class CacheEntryJsonConverterTests
    {
        [TestMethod]
        public void Read_InvalidJson_ThrowsJsonException()
        {
            var json = "[]";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void Read_MissingTypeProperty_ThrowsJsonException()
        {
            var json = "{\"Expiration\":1234567890,\"Data\":\"some data\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void Read_InvalidTypeProperty_ThrowsJsonException()
        {
            var json = "{\"Type\":\"Invalid.Type\",\"Expiration\":1234567890,\"Data\":\"some data\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void Read_TypeBeforeData_ThrowsJsonException()
        {
            var json = "{\"Data\":\"some data\",\"Type\":\"System.String\",\"Expiration\":1234567890}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }
    }
}
