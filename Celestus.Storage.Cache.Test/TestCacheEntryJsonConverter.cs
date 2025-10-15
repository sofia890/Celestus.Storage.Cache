using Celestus.Io;
using Celestus.Serialization;
using Celestus.Storage.Cache.Test.Model;
using static Celestus.Storage.Cache.CacheEntry;

namespace Celestus.Storage.Cache.Test
{
    [TestClass]
    public class TestCacheEntryJsonConverter
    {
        [TestMethod]
        public void VerifyThatWrongBaseTypeCausesCrash()
        {
            var json = "[]";
            Assert.ThrowsException<JsonException>(() => SerializationHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatMissingTypeCausesCrash()
        {
            var json = "{\"Expiration\":1234567890,\"Data\":\"some data\"}";
            Assert.ThrowsException<PropertiesOutOfOrderJsonException>(() => SerializationHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatMissingDataCausesCrash()
        {
            var json = """
                {
                    "Type":"System.String",
                    "Expiration":1234567890
                }
                """;
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatMissingExpirationCausesCrash()
        {
            var json = "{\"Type\":\"System.String\",\"Data\":\"some data\"}";
            Assert.ThrowsException<MissingValueJsonException>(() => SerializationHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatInvalidTypeCausesCrash()
        {
            var json = """
                {
                    "Type":"Invalid.Type",
                    "Expiration":1234567890,
                    "Data":"some data"
                }
                """;
            Assert.ThrowsException<NotObjectTypeJsonException>(() => SerializationHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatDataBeforeTypeCausesCrash()
        {
            var json = """
                {
                    "Data":"some data",
                    "Type":"System.String",
                    "Expiration":1234567890
                }
                """;
            Assert.ThrowsException<PropertiesOutOfOrderJsonException>(() => SerializationHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }

        [TestMethod]
        public void VerifyThatUnexpectedPropertyIsIgnored()
        {
            var json = """
                {
                    "SomethingMore": 5,
                    "Type":"System.String",
                    "Data":"some data",
                    "Expiration":1234567890
                }
                """;
            SerializationHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json);
        }

        [TestMethod]
        public void VerifyThatNullCanBeSerialized()
        {
            //
            // Arrange
            //
            CacheEntry entry = new(DateTime.MinValue, null);

            //
            // Act
            //
            using var file = new TempFile();

            Serialize.SaveToFile(entry, file.Info);

            var json = File.ReadAllText(file.Info.FullName);

            //
            // Assert
            //
            Assert.AreEqual("{\"Type\":null,\"Expiration\":0,\"Data\":null}", json);
        }

        [TestMethod]
        public void VerifyThatNullCanBeDeserialized()
        {
            var json = """
                {
                    "Type":"System.String",
                    "Expiration":0,
                    "Data":null
                }
                """;
            SerializationHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json);
        }

        [TestMethod]
        public void VerifyThatDataDeserializationFailureCausesCrash()
        {
            var json = $$"""
                {
                    "Type":"{{typeof(AlwaysDeserializeToNull).AssemblyQualifiedName}}",
                    "Expiration":0,
                    "Data":5
                }
                """;
            Assert.ThrowsException<ObjectCorruptJsonException>(() => SerializationHelper.Deserialize<CacheEntryJsonConverter, CacheEntry>(json));
        }
    }
}

