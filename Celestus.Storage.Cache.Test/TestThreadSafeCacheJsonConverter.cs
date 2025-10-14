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
                    "UnknownProperty":"value",
                    "Cache":{
                        "Type": "{{typeof(Cache).AssemblyQualifiedName}}",
                        "Content": {
                            "Id":"a",
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
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<ThreadSafeCacheJsonConverter, ThreadSafeCache>(json));
        }

        /// <summary>
        /// There used to be an ID property on the ThreadSafeCache itself. Test that it no longer is used.
        /// </summary>
        [TestMethod]
        public void VerifyThatInnerIdIsUsed()
        {
            const string INNER_ID = "";

            var json = $$"""
                {
                    "Id":"legacy-id",
                    "Cache":{
                        "Type": "{{typeof(Cache).AssemblyQualifiedName}}",
                        "Content": {
                            "Id":"{{INNER_ID}}",
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

            var cache = SerializationHelper.DeserializeAndReturn<ThreadSafeCacheJsonConverter, ThreadSafeCache>(json);

            Assert.AreEqual(INNER_ID, cache.Id);
            Assert.AreEqual(INNER_ID, cache.Cache.Id);
        }
    }
}
