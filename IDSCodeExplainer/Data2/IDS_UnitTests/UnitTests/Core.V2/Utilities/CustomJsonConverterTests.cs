using IDS.Core.V2.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class CustomJsonConverterTests
    {
        public class Sample
        {
            [JsonConverter(typeof(PropertiesConverter))]
            public object Properties { get; set; }
        }

        public class AProperties
        {
            public bool IsCircle { get; set; }
        }

        public class BProperties
        {
            public long Edge { get; set; }
        }

        public class PropertiesConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                switch (value)
                {
                    case AProperties a:
                        JToken.FromObject(a).WriteTo(writer);
                        break;
                    case BProperties b:
                        JToken.FromObject(b).WriteTo(writer);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.Null)
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        JObject jo = JObject.Load(reader);
                        if (jo.ContainsKey(nameof(AProperties.IsCircle)))
                        {
                            return jo.ToObject<AProperties>();
                        }
                        if (jo.ContainsKey(nameof(BProperties.Edge)))
                        {
                            return jo.ToObject<BProperties>();
                        }
                    }
                }

                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(AProperties) ||
                       objectType == typeof(BProperties);
            }
        }

        public class Sample2
        {
            [JsonConverter(typeof(VersionConverter))]
            public Version Version { get; set; }
        }

        private const string APropertiesSampleJson = "{\"Properties\":{\"IsCircle\":true}}";
        private const string BPropertiesSampleJson = "{\"Properties\":{\"Edge\":123}}";

        private const string APropertiesSampleBsonBase64 = "IQAAAANQcm9wZXJ0aWVzABAAAAAISXNDaXJjbGUAAQAA";
        private const string BPropertiesSampleBsonBase64 = "JAAAAANQcm9wZXJ0aWVzABMAAAASRWRnZQB7AAAAAAAAAAAA";

        private static Sample APropertiesSample => new Sample
        {
            Properties = new AProperties()
            {
                IsCircle = true
            }
        };

        private static Sample BPropertiesSample => new Sample
        {
            Properties = new BProperties()
            {
                Edge = 123
            }
        };

        private void AssertSampleTypeA(Sample sample)
        {
            Assert.IsNotNull(sample);
            Assert.IsNotNull(sample.Properties);
            var a = (AProperties)sample.Properties;
            Assert.IsNotNull(a);
            Assert.IsTrue(a.IsCircle);
        }

        private void AssertSampleTypeB(Sample sample)
        {
            Assert.IsNotNull(sample);
            Assert.IsNotNull(sample.Properties);
            var b = (BProperties)sample.Properties;
            Assert.IsNotNull(b);
            Assert.AreEqual(123, b.Edge);
        }

        #region JSON
        [TestMethod]
        public void Json_Serialize_Deserialize_Version_Test()
        {
            // Arrange
            var sample2 = new Sample2()
            {
                Version = new Version(1, 23, 456, 789)
            };
            // Act
            var json = JsonUtilities.Serialize(sample2);
            var version = JsonUtilities.Deserialize<Sample2>(json).Version;
            // Assert
            Assert.AreEqual(sample2.Version, version);
        }

        [TestMethod]
        public void Json_Serialize_A_Properties_Test()
        {
            // Arrange
            var user = APropertiesSample;

            // Act
            var json = JsonUtilities.Serialize(user);

            // Assert
            Assert.AreEqual(APropertiesSampleJson, json);
        }

        [TestMethod]
        public void Json_Serialize_B_Properties_Test()
        {
            // Arrange
            var user = BPropertiesSample;

            // Act
            var json = JsonUtilities.Serialize(user);

            // Assert
            Assert.AreEqual(BPropertiesSampleJson, json);
        }

        [TestMethod]
        public void Json_Deserialize_A_Properties_Test()
        {
            // Arrange & Act
            var sample = JsonUtilities.Deserialize<Sample>(APropertiesSampleJson);
            // Assert
            AssertSampleTypeA(sample);
        }

        [TestMethod]
        public void Json_Deserialize_B_Properties_Test()
        {
            // Arrange & Act
            var sample = JsonUtilities.Deserialize<Sample>(BPropertiesSampleJson);
            // Assert
            AssertSampleTypeB(sample);
        }
        #endregion

        #region BSON
        [TestMethod]
        public void Bson_Serialize_Deserialize_Version_Test()
        {
            // Arrange
            var sample2 = new Sample2()
            {
                Version = new Version(1, 23, 456, 789)
            };
            // Act
            var bytes = BsonUtilities.Serialize(sample2);
            var version = BsonUtilities.Deserialize<Sample2>(bytes).Version;
            // Assert
            Assert.AreEqual(sample2.Version, version);
        }

        [TestMethod]
        public void Bson_Serialize_A_Properties_Test()
        {
            // Arrange
            var user = APropertiesSample;

            // Act
            var bson = BsonUtilities.Serialize(user);
            var base64 = Convert.ToBase64String(bson);

            // Assert
            Assert.AreEqual(APropertiesSampleBsonBase64, base64);
        }

        [TestMethod]
        public void Bson_Serialize_B_Properties_Test()
        {
            // Arrange
            var user = BPropertiesSample;

            // Act
            var bson = BsonUtilities.Serialize(user);
            var base64 = Convert.ToBase64String(bson);

            // Assert
            Assert.AreEqual(BPropertiesSampleBsonBase64, base64);
        }

        [TestMethod]
        public void Bson_Deserialize_A_Properties_Test()
        {
            // Arrange
            var bson = Convert.FromBase64String(APropertiesSampleBsonBase64);
            // Act
            var sample = BsonUtilities.Deserialize<Sample>(bson);
            // Assert
            AssertSampleTypeA(sample);
        }

        [TestMethod]
        public void Bson_Deserialize_B_Properties_Test()
        {
            // Arrange
            var bson = Convert.FromBase64String(BPropertiesSampleBsonBase64);
            // Act
            var sample = BsonUtilities.Deserialize<Sample>(bson);
            // Assert
            AssertSampleTypeB(sample);
        }
        #endregion
    }
}
