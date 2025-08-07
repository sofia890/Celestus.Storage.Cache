using Celestus.Storage.Cache.Test.Model;
using static Celestus.Storage.Cache.ThreadCache;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestThreadCacheJsonConverter
    {
        [TestMethod]
        public void VerifyThatInvalidJsonCausesCrash()
        {
            var json = """
                "Invalid JSON"
                """;
            Assert.ThrowsException<StartTokenJsonException>(() => SerializationHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }

        [TestMethod]
        public void VerifyThatUnexpectedPropertyNameIsIgnored()
        {
            var json = """
                {
                    "UnknownProperty":"value",
                    "Key":"key",
                    "_cache":{
                        "Key":"key",
                        "_storage": {},
                        "Cleaner":{
                            "Type":"Celestus.Storage.Cache.CacheCleaner`1[[System.String, System.Private.CoreLib]], Celestus.Storage.Cache",
                            "Content":{
                                "_cleanupIntervalInTicks": 0
                            }
                        }
                    }
                }
                """;
            SerializationHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json);
        }

        [TestMethod]
        public void VerifyThatMissingCacheCausesCrash()
        {
            var json = """
                {
                    "Key":"key"
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingKeyCausesCrash()
        {
            var json = """
                {
                    "_cache":null
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }
    }
}
