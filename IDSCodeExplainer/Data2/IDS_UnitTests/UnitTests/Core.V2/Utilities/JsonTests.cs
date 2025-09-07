using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class JsonTests
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

        public class Properties
        {
            private List<Guid> _guids;

            public List<string> Guids
            {
                get
                {
                    return _guids?.Select(s => s.ToString()).ToList();
                }
                set
                {
                    if (value == null)
                    {
                        _guids = null;
                        return;
                    }

                    _guids = new List<Guid>();
                    foreach (var guidString in value)
                    {
                        if (Guid.TryParse(guidString, out var guid))
                        {
                            _guids.Add(guid);
                        }
                    }
                }
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

        public string SampleEventJson => "{\"Name\":\"Movie Premiere\",\"StartDate\":\"2013-01-22T20:30:00Z\"}";

        public string SampleCustomerWithJsonPropertyJson => "{\"cust-num\":\"BG60938\",\"cust-name\":\"Bubba Gump Shrimp Company\"}";

        public string SampleCustomerWithoutJsonPropertyJson => "{\"CustomerNumber\":\"BG60938\",\"CustomerName\":\"Bubba Gump Shrimp Company\"}";

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
            var actualJson = JsonUtilities.Serialize(SampleEvent);
            // Assert
            Assert.AreEqual(SampleEventJson, actualJson, "JSON after serialized aren't match");
        }

        [TestMethod]
        public void Deserialize_Test()
        {
            // Arrange & Act
            var actualEvent = JsonUtilities.Deserialize<Event>(SampleEventJson);
            // Assert
            Assert.AreEqual(SampleEvent, actualEvent, "Event after deserialized aren't match");
        }

        [TestMethod]
        public void Repetitive_Test()
        {
            // Arrange & Act
            var actualEvent = JsonUtilities.Deserialize<Event>(SampleEventJson);
            var actualJson = JsonUtilities.Serialize(actualEvent);
            // Assert
            Assert.AreEqual(SampleEventJson, actualJson, "JSON after deserialized -> serialized aren't match");
        }

        [TestMethod]
        public void Serialize_Deserialize_GeT_Set_Properties_Test()
        {
            // Arrange
            var properties = new Properties()
            {
                Guids = new List<string>()
                {
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString()
                }
            };
            // Act
            var json = JsonUtilities.Serialize(properties);
            var propertiesDeserialized = JsonUtilities.Deserialize<Properties>(json);

            Assert.AreEqual(properties.Guids.Count, propertiesDeserialized.Guids.Count, "Guids count not match");
            for (var i = 0; i < propertiesDeserialized.Guids.Count; i++)
            {
                Assert.AreEqual(properties.Guids[i], propertiesDeserialized.Guids[i], $"Guids[{i}] not match");
            }
        }

        [TestMethod]
        public void Serialize_With_JsonProperty_Test()
        {
            // Arrange & Act
            var actualJson = JsonUtilities.Serialize(SampleCustomer);
            // Assert
            Assert.AreEqual(SampleCustomerWithJsonPropertyJson, actualJson, "JSON after serialized aren't match");
        }

        [TestMethod]
        public void Serialize_Without_JsonProperty_Test()
        {
            // Arrange & Act
            var actualJson = JsonUtilities.Serialize(SampleCustomer, Formatting.None, true);
            // Assert
            Assert.AreEqual(SampleCustomerWithoutJsonPropertyJson, actualJson, "JSON after serialized aren't match");
        }

        [TestMethod]
        public void Deserialize_With_JsonProperty_Test()
        {
            // Arrange & Act
            var actualCustomer = JsonUtilities.Deserialize<Customer>(SampleCustomerWithJsonPropertyJson);
            // Assert
            Assert.AreEqual(SampleCustomer, actualCustomer, "Event after deserialized aren't match");
        }

        [TestMethod]
        public void Deserialize_Without_JsonProperty_Test()
        {
            // Arrange & Act
            var actualCustomer = JsonUtilities.Deserialize<Customer>(SampleCustomerWithoutJsonPropertyJson, NullValueHandling.Ignore, true);
            // Assert
            Assert.AreEqual(SampleCustomer, actualCustomer, "Event after deserialized aren't match");
        }
    }
}
