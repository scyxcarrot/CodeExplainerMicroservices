using IDS.Core.V2.Databases;
using IDS.Core.V2.DataModels;
using IDS.Core.V2.Reflections;
using IDS.Core.V2.TreeDb.Interface;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace IDS.Testing.UnitTests.V2
{
    public class CollectionNameConstants
    {
        // TODO: Define a standard on naming all collections
        public const string DoubleCollectionName = "Double";
    }

    public class DoubleValueData : GenericValueData<double>
    {
        public DoubleValueData(Guid id, IEnumerable<Guid> parents, double value) :
            base(id, parents, value)
        {
        }
    }

    [DbCollectionName(CollectionNameConstants.DoubleCollectionName)]
    [DbCollectionVersion(2, 0, 0)]
    public class DoubleDbCollection : GenericLiteDbCollection<DoubleValueData>
    {
        public DoubleDbCollection(LiteDatabase database, string collectionName) :
            base(database, collectionName)
        {
        }

        public override void MapObject(IVersion savedVersion)
        {
            BsonMapper.Global.RegisterType
            (
                serialize: (value) =>
                {
                    var doc = new BsonDocument
                    {
                        [LiteDbKeyConstants.ColValueKey] = value.Value
                    };
                    SetIdAndParentsId(doc, value);
                    return doc;
                },
                deserialize: (bson) => new DoubleValueDataBuilder().Build(savedVersion, bson)
           );
        }
    }

    #region With Deserializer

    interface IDeserializer
    {
        IVersion Version { get; }
        ImmutableDictionary<string, object> Deserialize(BsonValue value);
        ImmutableDictionary<string, object> FromOlder(ImmutableDictionary<string, object> previousData);
    }

    internal class Constants
    {
        internal const string Property1Key = "Property1";
        internal const string Property2Key = "Property2";
    }

    internal class DeserializerDataV200 : IDeserializer
    {
        public IVersion Version => new DbCollectionVersion(2, 0, 0);

        public ImmutableDictionary<string, object> Deserialize(BsonValue value)
        {
            var doc = new Dictionary<string, object>
            {
                { LiteDbKeyConstants.ColIdKey, value[LiteDbKeyConstants.ColIdKey].AsGuid },
                { LiteDbKeyConstants.ColParentsKey, value[LiteDbKeyConstants.ColParentsKey].AsArray
                        ?.Select(v => v.AsGuid)
                        .ToImmutableList() },
                {LiteDbKeyConstants.ColValueKey, value[LiteDbKeyConstants.ColValueKey].AsDouble }
            };
            return doc.ToImmutableDictionary();
        }

        public ImmutableDictionary<string, object> FromOlder(ImmutableDictionary<string, object> previousData)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object>();
            builder.AddRange(previousData);
            builder.Remove(Constants.Property2Key);
            builder.Add(LiteDbKeyConstants.ColValueKey, 2.0);
            return builder.ToImmutable();
        }
    }

    internal class DeserializerDataV110 : IDeserializer
    {
        public IVersion Version => new DbCollectionVersion(1, 1, 0);

        public ImmutableDictionary<string, object> Deserialize(BsonValue value)
        {
            var doc = new Dictionary<string, object>
            {
                { LiteDbKeyConstants.ColIdKey, value[LiteDbKeyConstants.ColIdKey].AsGuid },
                { LiteDbKeyConstants.ColParentsKey, value[LiteDbKeyConstants.ColParentsKey].AsArray
                    ?.Select(v => v.AsGuid)
                    .ToImmutableList() },
                {Constants.Property2Key, value[Constants.Property2Key].AsString }
            };
            return doc.ToImmutableDictionary();
        }

        public ImmutableDictionary<string, object> FromOlder(ImmutableDictionary<string, object> previousData)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, object>();
            builder.AddRange(previousData);
            builder.Remove(Constants.Property1Key);
            builder.Add(Constants.Property2Key, "default");
            return builder.ToImmutable();
        }
    }

    internal class DeserializerDataV100 : IDeserializer
    {
        public IVersion Version => new DbCollectionVersion(1, 0, 0);

        public ImmutableDictionary<string, object> Deserialize(BsonValue value)
        {
            var doc = new Dictionary<string, object>
            {
                { LiteDbKeyConstants.ColIdKey, value[LiteDbKeyConstants.ColIdKey].AsGuid },
                { LiteDbKeyConstants.ColParentsKey, value[LiteDbKeyConstants.ColParentsKey].AsArray
                    ?.Select(v => v.AsGuid)
                    .ToImmutableList() },
                {Constants.Property1Key, value[Constants.Property1Key].AsString }
            };
            return doc.ToImmutableDictionary();
        }

        public ImmutableDictionary<string, object> FromOlder(ImmutableDictionary<string, object> previousData)
        {
            throw new NotImplementedException();
        }
    }

    public class DoubleValueDataBuilder
    {
        private readonly List<IDeserializer> deserializers;

        public DoubleValueDataBuilder()
        {
            //added in the order of oldest supported version to latest version
            deserializers = new List<IDeserializer>();
            deserializers.Add(new DeserializerDataV100());
            deserializers.Add(new DeserializerDataV110());
            deserializers.Add(new DeserializerDataV200());
        }

        public DoubleValueData Build(IVersion savedVersion, BsonValue bson)
        {
            var foundDeserializer = false;
            ImmutableDictionary<string, object> dictionary = null;

            foreach (var deserializer in deserializers)
            {
                if (!foundDeserializer && deserializer.Version.Equals(savedVersion))
                {
                    foundDeserializer = true;
                    dictionary = deserializer.Deserialize(bson);
                }
                else if (foundDeserializer)
                {
                    dictionary = deserializer.FromOlder(dictionary);
                }
            }

            return dictionary == null ? null :
                new DoubleValueData((Guid)dictionary[LiteDbKeyConstants.ColIdKey],
                (IEnumerable<Guid>)dictionary[LiteDbKeyConstants.ColParentsKey],
                (double)dictionary[LiteDbKeyConstants.ColValueKey]);
        }
    }

    #endregion

    [TestClass]
    public class LiteDbCollectionAndQueryTests
    {
        public static void CompareDoubleValueData(DoubleValueData expected, DoubleValueData actual)
        {
            Assert.AreEqual(expected.Id, actual.Id, "The Id isn't match");
            CollectionAssert.AreEquivalent(expected.Parents, actual.Parents, "The parent Ids aren't match");
            Assert.AreEqual(expected.Value, actual.Value, 0.000001, "The value isn't match");
        }

        [TestMethod]
        public void LiteDb_Collection_Read_Create_Test()
        {
            // Arrange
            using (var memoryStream = new MemoryStream())
            {
                using (var liteDb = new LiteDatabase(memoryStream))
                {
                    // Arrange
                    IDbCollection dbCollection = new DoubleDbCollection(liteDb, CollectionNameConstants.DoubleCollectionName);
                    // Act
                    dbCollection.MapObject(new DbCollectionVersion(2, 0, 0));
                    var originalData = new DoubleValueData(Guid.NewGuid(), new List<Guid>() { Guid.NewGuid() }, 1.234567);
                    var createSuccess = dbCollection.Create(originalData);
                    var readData = dbCollection.Read(originalData.Id) as DoubleValueData ;
                    // Assert
                    Assert.IsTrue(createSuccess, "The data created successfully");
                    Assert.IsNotNull(readData, "The read data shouldn't be null");
                    CompareDoubleValueData(originalData, readData);
                }
            }
        }

        [TestMethod]
        public void LiteDb_Collection_Delete_Test()
        {
            // Arrange
            using (var memoryStream = new MemoryStream())
            {
                using (var liteDb = new LiteDatabase(memoryStream))
                {
                    // Arrange
                    IDbCollection dbCollection = new DoubleDbCollection(liteDb, CollectionNameConstants.DoubleCollectionName);
                    // Act
                    dbCollection.MapObject(new DbCollectionVersion(2, 0, 0));
                    var originalData = new DoubleValueData(Guid.NewGuid(), new List<Guid>() { Guid.NewGuid() }, 1.234567);
                    var createSuccess = dbCollection.Create(originalData);
                    var deletedData = dbCollection.Delete(originalData.Id) as DoubleValueData;
                    var readData = dbCollection.Read(originalData.Id) as DoubleValueData;
                    // Assert
                    Assert.IsTrue(createSuccess, "The data created successfully");
                    Assert.IsNotNull(deletedData, "The deleted data shouldn't be null");
                    Assert.IsNull(readData, "The read data should be null");
                    CompareDoubleValueData(originalData, deletedData);
                }
            }
        }

        [TestMethod]
        public void LiteDb_Collection_ReadAll_Test()
        {
            // Arrange
            using (var memoryStream = new MemoryStream())
            {
                using (var liteDb = new LiteDatabase(memoryStream))
                {
                    // Arrange
                    IDbCollection dbCollection = new DoubleDbCollection(liteDb, CollectionNameConstants.DoubleCollectionName);
                    // Act
                    dbCollection.MapObject(new DbCollectionVersion(2, 0, 0));
                    var data1 = new DoubleValueData(Guid.NewGuid(), new List<Guid>() { Guid.NewGuid() }, 1.234567);
                    var createSuccess1 = dbCollection.Create(data1); 
                    var data2 = new DoubleValueData(Guid.NewGuid(), new List<Guid>() { Guid.NewGuid() }, 9.8765432);
                    var createSuccess2 = dbCollection.Create(data2);
                    var allData = dbCollection.ReadAll();
                    // Assert
                    Assert.IsTrue(createSuccess1, "First data created successfully");
                    Assert.IsTrue(createSuccess2, "Second data created successfully");
                    Assert.AreEqual(2, allData.Count, "ReadAll should return 2 data");
                    CompareDoubleValueData(data1, (DoubleValueData)allData.First(d => d.Id == data1.Id));
                    CompareDoubleValueData(data2, (DoubleValueData)allData.First(d => d.Id == data2.Id));
                }
            }
        }

        [TestMethod]
        public void LiteDb_Collection_Query_Load_Assembly_Test()
        {
            // Arrange
            using (var memoryStream = new MemoryStream())
            {
                using (var liteDb = new LiteDatabase(memoryStream))
                {
                    // Arrange
                    var collectionQuery = new LiteDbCollectionQuery(liteDb, AppDomain.CurrentDomain.GetAssemblies());
                    // Act
                    var collections = collectionQuery.GetAllDbCollection();
                    // Assert
                    Assert.IsTrue(collections.Any(c => c.Name == CollectionNameConstants.DoubleCollectionName), "Failed to find the double db collection");
                }
            }
        }

        [TestMethod]
        public void Collection_Query_With_Data_Test()
        {
            // Arrange
            using (var memoryStream = new MemoryStream())
            {
                using (var liteDb = new LiteDatabase(memoryStream))
                {
                    // Arrange
                    var collectionQuery = new LiteDbCollectionQuery(liteDb, AppDomain.CurrentDomain.GetAssemblies());
                    // Act
                    var data = new DoubleValueData(Guid.NewGuid(), new List<Guid>() { Guid.NewGuid() }, 1.234567);
                    var querySuccess = collectionQuery.GetDbCollection(data, out var dbCollection);
                    // Assert
                    Assert.IsTrue(querySuccess, "Failed to query the db collection");
                    Assert.IsNotNull(dbCollection, "Db Collection is null");
                    Assert.AreEqual(CollectionNameConstants.DoubleCollectionName, dbCollection.Name, "Might found a wrong db collection");
                }
            }
        }

        [TestMethod]
        public void Collection_Query_With_Type_Test()
        {
            // Arrange
            using (var memoryStream = new MemoryStream())
            {
                using (var liteDb = new LiteDatabase(memoryStream))
                {
                    // Arrange
                    var collectionQuery = new LiteDbCollectionQuery(liteDb, AppDomain.CurrentDomain.GetAssemblies());
                    // Act
                    var querySuccess = collectionQuery.GetDbCollection(typeof(DoubleValueData), out var dbCollection);
                    // Assert
                    Assert.IsTrue(querySuccess, "Failed to query the db collection");
                    Assert.IsNotNull(dbCollection, "Db Collection is null");
                    Assert.AreEqual(CollectionNameConstants.DoubleCollectionName, dbCollection.Name, "Might found a wrong db collection");
                }
            }
        }

        [TestMethod]
        public void DeserializerDataV100_Test()
        {
            // Arrange
            using (var memoryStream = new MemoryStream())
            {
                using (var liteDb = new LiteDatabase(memoryStream))
                {
                    // Arrange
                    var collection = liteDb.GetCollection(CollectionNameConstants.DoubleCollectionName);
                    var doc = new BsonDocument
                    {
                        [LiteDbKeyConstants.ColIdKey] = Guid.NewGuid(),
                        [LiteDbKeyConstants.ColParentsKey] = new BsonArray(new List<BsonValue> { new BsonValue(Guid.NewGuid()) }),
                        [Constants.Property1Key] = "Property1Value"
                    };
                    collection.Insert(doc);
                    var metadataCollection = liteDb.GetCollection<DbComponentMetadata>(DbCollectionNameConstants.MetadataCollectionName);
                    var keyValuePairs = new Dictionary<string, string>();
                    keyValuePairs.Add(CollectionNameConstants.DoubleCollectionName, new DbCollectionVersion(1, 0, 0).ToString());
                    metadataCollection.Insert(new DbComponentMetadata
                    {
                        KeyValuePairs = keyValuePairs
                    });

                    // Act
                    var collectionQuery = new LiteDbCollectionQuery(liteDb, AppDomain.CurrentDomain.GetAssemblies());
                    var querySuccess = collectionQuery.GetDbCollection(typeof(DoubleValueData), out var dbCollection);
                    var allData = dbCollection.ReadAll();

                    // Assert
                    Assert.IsTrue(querySuccess, "Failed to query the db collection");
                    Assert.IsNotNull(dbCollection, "Db Collection is null");
                    Assert.IsTrue(allData.Count == 1, "There should be 1 item");
                    Assert.AreEqual(2.0, ((DoubleValueData)allData[0]).Value, "Value should be 2.0");
                }
            }
        }

        [TestMethod]
        public void DeserializerDataV110_Test()
        {
            // Arrange
            using (var memoryStream = new MemoryStream())
            {
                using (var liteDb = new LiteDatabase(memoryStream))
                {
                    // Arrange
                    var collection = liteDb.GetCollection(CollectionNameConstants.DoubleCollectionName);
                    var doc = new BsonDocument
                    {
                        [LiteDbKeyConstants.ColIdKey] = Guid.NewGuid(),
                        [LiteDbKeyConstants.ColParentsKey] = new BsonArray(new List<BsonValue> { new BsonValue(Guid.NewGuid()) }),
                        [Constants.Property2Key] = "Property2Value"
                    };
                    collection.Insert(doc);
                    var metadataCollection = liteDb.GetCollection<DbComponentMetadata>(DbCollectionNameConstants.MetadataCollectionName);
                    var keyValuePairs = new Dictionary<string, string>();
                    keyValuePairs.Add(CollectionNameConstants.DoubleCollectionName, new DbCollectionVersion(1, 1, 0).ToString());
                    metadataCollection.Insert(new DbComponentMetadata
                    {
                        KeyValuePairs = keyValuePairs
                    });

                    // Act
                    var collectionQuery = new LiteDbCollectionQuery(liteDb, AppDomain.CurrentDomain.GetAssemblies());
                    var querySuccess = collectionQuery.GetDbCollection(typeof(DoubleValueData), out var dbCollection);
                    var allData = dbCollection.ReadAll();

                    // Assert
                    Assert.IsTrue(querySuccess, "Failed to query the db collection");
                    Assert.IsNotNull(dbCollection, "Db Collection is null");
                    Assert.IsTrue(allData.Count == 1, "There should be 1 item");
                    Assert.AreEqual(2.0, ((DoubleValueData)allData[0]).Value, "Value should be 2.0");
                }
            }
        }
    }
}
