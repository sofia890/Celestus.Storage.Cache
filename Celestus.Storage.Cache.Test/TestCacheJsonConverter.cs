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
            Assert.ThrowsException<WrongTokenJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatUnexpectedPropertyNameAreSkipped()
        {
            string json = $$"""
                {
                    "InvalidProperty":"value",
                    "Id":"a id",
                    "Storage":{},
                    "Cleaner":{
                        "Type":"{{typeof(CacheCleaner<string, string>).AssemblyQualifiedName}}",
                        "Content":{
                            "CleanupInterval": "00:00:00.5"
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
                    "Id":"a id"
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
            string json = $$"""
                {
                    "Id": "a",
                    "Storage": {
                    },
                    "Cleaner":{
                        "Type":"{{typeof(DummyClass).AssemblyQualifiedName}}",
                        "Content":{
                            "PlaceHolder": 5
                        }
                    }
                }
                """;
            Assert.ThrowsException<MissingInheritanceJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatIncompleteConfigurationForCleanerCausesCrash()
        {
            string json = $$"""
                {
                    "Cleaner":{
                        "Type":"{{typeof(CacheCleaner<string, string>).AssemblyQualifiedName}}",
                        "Content":{}
                    }
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }

        [TestMethod]
        public void VerifyThatExtraCleanerParameterIsIgnored()
        {
            string json = $$"""
                {
                    "Id":"a id",
                    "Storage":{},
                    "Cleaner":{
                        "Extra":1,
                        "Type":"{{typeof(CacheCleaner<string, string>).AssemblyQualifiedName}}",
                        "Content":{
                            "CleanupInterval": "00:00:00.5"
                        }
                    }
                }
                """;
            SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json);
        }

        [TestMethod]
        public void VerifyThatMissingConfigurationForCleanerCausesCrash()
        {
            string json = $$"""
                {
                    "Cleaner":{
                        "Type":"{{typeof(CacheCleaner<string, string>).AssemblyQualifiedName}}"
                    }
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<CacheJsonConverter, Cache>(json));
        }
    }
}
