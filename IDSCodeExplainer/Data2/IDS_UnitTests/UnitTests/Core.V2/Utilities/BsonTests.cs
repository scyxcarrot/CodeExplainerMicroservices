using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class BsonTests
    {
        public class Event
        {
            public string Name { get; set; }
            // this converter is much repetitive compare to default converter 
            [JsonConverter(typeof(IsoDateTimeConverter))]
            public DateTime StartDate { get; set; }

            public override bool Equals(object obj)
            {
                return obj is Event other &&
                       Name == other.Name &&
                       StartDate.Equals(other.StartDate);
            }
        }

        public class Customer
        {
            [JsonProperty("cust-num")]
            public string CustomerNumber { get; set; }
            [JsonProperty("cust-name")]
            public string CustomerName { get; set; }

            public override bool Equals(object obj)
            {
                return obj is Customer other &&
                       CustomerNumber == other.CustomerNumber &&
                       CustomerName.Equals(other.CustomerName);
            }
        }

        public string Base64Byte => "QgAAAAJOYW1lAA8AAABNb3ZpZSBQcmVtaWVyZQACU3RhcnREYXRlABUAAAAyMDEzLTAxLTIyVDIwOjMwOjAwWgAA";

        public Event SampleEvent => new Event()
        {
            Name = "Movie Premiere",
            StartDate = new DateTime(2013, 1, 22, 20, 30, 0, DateTimeKind.Utc)
        };

        public Customer SampleCustomer => new Customer
        {
            CustomerName = "Bubba Gump Shrimp Company",
            CustomerNumber = "BG60938"
        };

        [TestMethod]
        public void Serialize_Test()
        {
            // Arrange & Act
            var actualBase64 = Convert.ToBase64String(BsonUtilities.Serialize(SampleEvent));

            Assert.AreEqual(Base64Byte, actualBase64, "Base 64 string after serialized aren't match");
        }

        [TestMethod]
        public void Deserialize_Test()
        {
            // Arrange
            var data = Convert.FromBase64String(Base64Byte);
            // Act
            var actualEvent = BsonUtilities.Deserialize<Event>(data);
            // Assert
            Assert.AreEqual(SampleEvent, actualEvent, "Event after deserialized aren't match");
        }

        [TestMethod]
        public void Repetitive_Test()
        {
            // Arrange
            var data = Convert.FromBase64String(Base64Byte);
            // Act
            var actualEvent = BsonUtilities.Deserialize<Event>(data);
            var actualBase64 = Convert.ToBase64String(BsonUtilities.Serialize(actualEvent));
            // Assert
            Assert.AreEqual(Base64Byte, actualBase64, "Base 64 string after deserialized -> serialized aren't match");
        }

        [TestMethod]
        public void Repetitive_With_JSONProperty_Test()
        {
            // Arrange & Act
            var bson = BsonUtilities.Serialize(SampleCustomer);
            var customer = BsonUtilities.Deserialize<Customer>(bson);
            // Assert
            Assert.AreEqual(SampleCustomer, customer, "BSON after deserialized -> serialized aren't match");
        }

        [TestMethod]
        public void Repetitive_Without_JSONProperty_Test()
        {
            // Arrange & Act
            var bson = BsonUtilities.Serialize(SampleCustomer, true);
            var customer = BsonUtilities.Deserialize<Customer>(bson, true);
            // Assert
            Assert.AreEqual(SampleCustomer, customer, "BSON after deserialized -> serialized aren't match");
        }

        [TestMethod]
        public void Repetitive_Mismatch_JSONProperty_Test()
        {
            // Arrange & Act
            var bson = BsonUtilities.Serialize(SampleCustomer, true);
            var customer = BsonUtilities.Deserialize<Customer>(bson);
            // Assert
            Assert.AreNotEqual(SampleCustomer, customer, "BSON after deserialized -> serialized are match");
        }

        [TestMethod]
        public void JSONProperty_Length_Comparison_Test()
        {
            // Arrange & Act
            var bsonShortPropertyName = BsonUtilities.Serialize(SampleCustomer);
            var bsonLongPropertyName = BsonUtilities.Serialize(SampleCustomer, true);
            // Assert
            Assert.AreNotEqual(bsonShortPropertyName.Length, bsonLongPropertyName.Length, "BSON length shouldn't same with different properties name");
            Assert.IsTrue(bsonLongPropertyName.Length > bsonShortPropertyName.Length, "BSON with long property name should have longer Byte than the short one");
        }
    }
}
