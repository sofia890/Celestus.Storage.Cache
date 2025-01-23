using System.Text.Json;
using static Celestus.Storage.Cache.ThreadCache;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class ThreadThreadCacheJsonConverterTests
    {

        [TestMethod]
        public void VerifyThatInvalidJsonCausesCrash()
        {
            var json = "\"Invalid JSON\"";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }

        [TestMethod]
        public void VerifyThatUnexpectedPropertyNameCausesCrash()
        {
            var json = "{\"InvalidProperty\":\"value\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingCacheCausesCrash()
        {
            var json = "{\"Key\":\"key\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingKeyCausesCrash()
        {
            var json = "{\"_cache\":null}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }
    }
}
