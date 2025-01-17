using System.Text.Json;
using static Celestus.Storage.Cache.ThreadCache;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class ThreadThreadCacheJsonConverterTests
    {

        [TestMethod]
        public void Read_ShouldThrowJsonException_WhenTokenTypeIsNotStartObject()
        {
            var json = "\"Invalid JSON\"";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }

        [TestMethod]
        public void Read_ShouldThrowJsonException_WhenPropertyNameIsInvalid()
        {
            var json = "{\"InvalidProperty\":\"value\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }

        [TestMethod]
        public void Read_ShouldThrowJsonException_WhenCacheIsMissing()
        {
            var json = "{\"Key\":\"key\"}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }

        [TestMethod]
        public void Read_ShouldThrowJsonException_WhenKeyIsMissing()
        {
            var json = "{\"_cache\":null}";
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }
    }
}
