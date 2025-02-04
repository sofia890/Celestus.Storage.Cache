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
            string json = """
                "Invalid JSON"
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatUnexpectedPropertyNameCausesCrash()
        {
            string json = """
                {
                    "InvalidProperty":"value"
                }
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingStorageCausesCrash()
        {
            string json = """
                {
                    "Key":"key"
                }
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingKeyCausesCrash()
        {
            string json = """
                {
                    "_storage":null
                }
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatEmptyCleanerCausesCrash()
        {
            string json = """
                {
                    "_cleaner":{}
                }
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingCleanerTypeCausesCrash()
        {
            string json = """
                {
                    "_cleaner":{
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatNullForCleanerTypeCausesCrash()
        {
            string json = """
                {
                    "_cleaner":{
                        "Type":null,
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatInvalidCleanerTypeCausesCrash()
        {
            string json = """
                {
                    "_cleaner":{
                        "Type":"InvalidType",
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatFailureToCreateCleanerCausesCrash()
        {
            string json = """
                {
                    "_cleaner":{
                        "Type":"System.Object, System.Private.CoreLib",
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatIncompleteConfigurationForCleanerCausesCrash()
        {
            string json = """
                {
                    "_cleaner":{
                        "Type":"Celestus.Storage.Cache.CacheCleaner`1[[System.String, System.Private.CoreLib]], Celestus.Storage.Cache",
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatExtraCleanerParametersAreIgnored()
        {
            string json = """
                {
                    "Key":"key",
                    "_storage":{},
                    "_cleaner":{
                        "Extra":1,
                        "Type":"Celestus.Storage.Cache.CacheCleaner`1[[System.String, System.Private.CoreLib]], Celestus.Storage.Cache",
                        "Content":{
                            "_cleanupIntervalInTicks": 0
                        }
                    }
                }
                """;
            ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json);
        }

        [TestMethod]
        public void VerifyThatMissingConfigurationForCleanerCausesCrash()
        {
            string json = """
                {
                    "_cleaner":{
                        "Type":"Celestus.Storage.Cache.CacheCleaner`1[[System.String, System.Private.CoreLib]], Celestus.Storage.Cache"
                    }
                }
                """;
            Assert.ThrowsException<JsonException>(() => ExceptionHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }
    }
}
