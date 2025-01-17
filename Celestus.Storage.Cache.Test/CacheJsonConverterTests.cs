using System.Text.Json;
using static Celestus.Storage.Cache.Cache;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class CacheJsonConverterTests
    {
        [TestMethod]
        public void Read_ShouldThrowJsonException_WhenTokenTypeIsNotStartObject()
        {
            var json = "\"Invalid JSON\"";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void Read_ShouldThrowJsonException_WhenPropertyNameIsInvalid()
        {
            var json = "{\"InvalidProperty\":\"value\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void Read_ShouldThrowJsonException_WhenStorageIsMissing()
        {
            var json = "{\"Key\":\"key\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void Read_ShouldThrowJsonException_WhenKeyIsMissing()
        {
            var json = "{\"_storage\":null}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }
    }
}
