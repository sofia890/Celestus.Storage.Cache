using Celestus.Storage.Cache.Test.Model;

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
            Assert.ThrowsException<StartTokenJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatUnexpectedPropertyNameAreSkipped()
        {
            string json = """
                {
                    "InvalidProperty":"value",
                    "Key":"key",
                    "Storage":{},
                    "Cleaner":{
                        "Type":"Celestus.Storage.Cache.CacheCleaner`1[[System.String, System.Private.CoreLib]], Celestus.Storage.Cache",
                        "Content":{
                            "_cleanupIntervalInTicks": 0
                        }
                    }
                }
                """;
            SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json);
        }

        [TestMethod]
        public void VerifyThatMissingStorageCausesCrash()
        {
            string json = """
                {
                    "Key":"key"
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingKeyCausesCrash()
        {
            string json = """
                {
                    "Storage":null
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatEmptyCleanerCausesCrash()
        {
            string json = """
                {
                    "Cleaner":{}
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatMissingCleanerTypeCausesCrash()
        {
            string json = """
                {
                    "Cleaner":{
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<PropertiesOutOfOrderJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatNullForCleanerTypeCausesCrash()
        {
            string json = """
                {
                    "Cleaner":{
                        "Type":null,
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<ValueTypeJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatInvalidCleanerTypeCausesCrash()
        {
            string json = """
                {
                    "Cleaner":{
                        "Type":"InvalidType",
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<NotObjectTypeJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatFailureToCreateCleanerCausesCrash()
        {
            string json = """
                {
                    "Cleaner":{
                        "Type":"System.Object, System.Private.CoreLib",
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<MissingInheritanceJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatIncompleteConfigurationForCleanerCausesCrash()
        {
            string json = """
                {
                    "Cleaner":{
                        "Type":"Celestus.Storage.Cache.CacheCleaner`1[[System.String, System.Private.CoreLib]], Celestus.Storage.Cache",
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatExtraCleanerParameterIsIgnored()
        {
            string json = """
                {
                    "Key":"key",
                    "Storage":{},
                    "Cleaner":{
                        "Extra":1,
                        "Type":"Celestus.Storage.Cache.CacheCleaner`1[[System.String, System.Private.CoreLib]], Celestus.Storage.Cache",
                        "Content":{
                            "_cleanupIntervalInTicks": 0
                        }
                    }
                }
                """;
            SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json);
        }

        [TestMethod]
        public void VerifyThatMissingConfigurationForCleanerCausesCrash()
        {
            string json = """
                {
                    "Cleaner":{
                        "Type":"Celestus.Storage.Cache.CacheCleaner`1[[System.String, System.Private.CoreLib]], Celestus.Storage.Cache"
                    }
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }
    }
}
