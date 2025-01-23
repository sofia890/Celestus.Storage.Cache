using System.Text.Json;
using static Celestus.Storage.Cache.Cache;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestCacheJsonConverter
    {
        [TestMethod]
        public void VerifyThatInvalidJsonCausesCrash()
        {
            var json = "\"Invalid JSON\"";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatUnexpectedPropertyNameCausesCrash()
        {
            var json = "{\"InvalidProperty\":\"value\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingStorageCausesCrash()
        {
            var json = "{\"Key\":\"key\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingKeyCausesCrash()
        {
            var json = "{\"_storage\":null}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }
    }
}
