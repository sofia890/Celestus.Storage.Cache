using Celestus.Storage.Cache.Test.Model;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestThreadSafeCacheJsonConverter
    {
        [TestMethod]
        public void VerifyThatInvalidJsonCausesCrash()
        {
            var json = """
                "Invalid JSON"
                """;
            Assert.ThrowsException<WrongTokenJsonException>(() => SerializationHelper.Deserialize<ThreadSafeCacheJsonConverter, ThreadSafeCache>(json));
        }

        [TestMethod]
        public void VerifyThatUnexpectedPropertyNameIsIgnored()
        {
            var json = $$"""
                {
                    "Id":"a",
                    "UnknownProperty":"value",
                    "Cache":{
                        "Type": "{{typeof(Cache).AssemblyQualifiedName}}",
                        "Content": {
                            "Id":"",
                            "Storage": {},
                            "Cleaner":{
                                "Type":"{{typeof(CacheCleaner<string, string>).AssemblyQualifiedName}}",
                                "Content":{
                                    "CleanupInterval": "00:00:00.5"
                                }
                            }
                        }
                    }
                }
                """;
            SerializationHelper.Deserialize<ThreadSafeCacheJsonConverter, ThreadSafeCache>(json);
        }

        [TestMethod]
        public void VerifyThatMissingCacheCausesCrash()
        {
            var json = """
                {
                    "Id":"a id"
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<ThreadSafeCacheJsonConverter, ThreadSafeCache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingKeyCausesCrash()
        {
            var json = """
                {
                    "Cache":{}
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<ThreadSafeCacheJsonConverter, ThreadSafeCache>(json));
        }
    }
}
