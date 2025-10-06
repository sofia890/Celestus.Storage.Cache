using Celestus.Storage.Cache.Test.Model;

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
            var json = $$"""
                {
                    "UnknownProperty":"value",
                    "Id":"a id",
                    "Cache":{
                        "Id":"a id",
                        "Storage": {},
                        "Cleaner":{
                            "Type":"{{typeof(CacheCleaner<string, string>).UnderlyingSystemType.FullName}}",
                            "Content":{
                                "_cleanupInterval": "00:00:00.5"
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
                    "Id":"a id"
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingKeyCausesCrash()
        {
            var json = """
                {
                    "Cache":null
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<ThreadCacheJsonConverter, ThreadCache>(json));
        }
    }
}
