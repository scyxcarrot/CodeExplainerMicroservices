using IDS.Core.V2.Databases;
using IDS.Core.V2.DataModels;
using IDS.Core.V2.TreeDb.Interface;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class IDatabaseTests
    {
        [TestMethod]
        public void Read_All_Data_Test()
        {
            using (var _ = CreateDatabase(out var database))
            {
                using (database)
                {
                    // Arrange
                    var data1 = new DoubleValueData(Guid.NewGuid(), new List<Guid>() { Guid.NewGuid() }, 1.234567);
                    var data2 = new DoubleValueData(Guid.NewGuid(), new List<Guid>() { Guid.NewGuid() }, 9.8765432);
                    // Act
                    var createSuccess1 = database.Create(data1);
                    var createSuccess2 = database.Create(data2);
                    var allData = database.ReadAll();
                    // Assert
                    Assert.IsTrue(createSuccess1, "First data should create successfully");
                    Assert.IsTrue(createSuccess2, "Second data should create successfully");
                    Assert.AreEqual(2, allData.Count, "Second data should create successfully");
                    LiteDbCollectionAndQueryTests.CompareDoubleValueData(data1, (DoubleValueData)allData.First(d => d.Id == data1.Id));
                    LiteDbCollectionAndQueryTests.CompareDoubleValueData(data2, (DoubleValueData)allData.First(d => d.Id == data2.Id));
                }
            }
        }

        [TestMethod]
        public void Database_Read_Create_Test()
        {
            using (var _ = CreateDatabase(out var database))
            {
                using (database)
                {
                    // Arrange
                    var doubleValueData = GetDoubleValueData();

                    // Act
                    var createSuccess = database.Create(doubleValueData);
                    var readData = (DoubleValueData) database.Read(doubleValueData.Id);

                    // Assert
                    Assert.IsTrue(createSuccess, "Data should be created successfully");
                    LiteDbCollectionAndQueryTests.CompareDoubleValueData(
                        doubleValueData, readData);
                }
            }
        }

        [TestMethod]
        public void Database_Should_Be_Able_To_Read_After_Closing_And_Reopen()
        {
            // Arrange
            var doubleValueData = GetDoubleValueData();

            // Act
            using (var memoryStream = CreateDatabase(out _))
            {
                using (var database = CreateDatabaseFromMemoryStream(memoryStream))
                {
                    var createSuccess = database.Create(doubleValueData);
                    Assert.IsTrue(createSuccess, "Data should be created successfully");
                }

                using (var databaseReopen = CreateDatabaseFromMemoryStream(memoryStream))
                {
                    databaseReopen.ReadAll();
                    var readData = (DoubleValueData)databaseReopen.Read(doubleValueData.Id);

                    // Assert
                    LiteDbCollectionAndQueryTests.CompareDoubleValueData(
                        doubleValueData, readData);
                }
            }
        }

        [TestMethod]
        public void Database_Delete_Test()
        {
            using (var _ = CreateDatabase(out var database))
            {
                using (database)
                {
                    // Arrange
                    var doubleValueData = GetDoubleValueData();

                    // Act
                    var createSuccess = database.Create(doubleValueData);
                    var deletedData = (DoubleValueData)database.Delete(doubleValueData.Id);
                    var readData = (DoubleValueData)database.Read(doubleValueData.Id);

                    // Assert
                    Assert.IsTrue(createSuccess, "Data should be created successfully");
                    LiteDbCollectionAndQueryTests.CompareDoubleValueData(
                        doubleValueData, deletedData);
                    Assert.IsNull(readData, 
                        "readData should be null because it is supposed to to be deleted");
                }
            }
        }

        [TestMethod]
        public void Database_Delete_Data_That_Dont_Exist_Test()
        {
            using (var _ = CreateDatabase(out var database))
            {
                using (database)
                {
                    // Arrange
                    var doubleValueData = GetDoubleValueData();

                    // Act
                    var createSuccess = database.Create(doubleValueData);
                    var deletedData = (DoubleValueData)database.Delete(Guid.NewGuid());
                    var readData = (DoubleValueData)database.Read(doubleValueData.Id);

                    // Assert
                    Assert.IsTrue(createSuccess, "Data should be created successfully");
                    Assert.IsNull(deletedData, 
                        "deletedData should be null because wrong GUID was given");
                    LiteDbCollectionAndQueryTests.CompareDoubleValueData(
                        doubleValueData, readData);
                }
            }
        }

        [TestMethod]
        public void Get_Metadata_Test()
        {
            using (var _ = CreateDatabase(out var database))
            {
                using (database)
                {
                    // Arrange
                    var data1 = new DoubleValueData(Guid.NewGuid(), new List<Guid>() { Guid.NewGuid() }, 1.234567);

                    // Act
                    database.Create(data1);
                    var allMetadata = database.GetMetadata();

                    // Assert
                    Assert.IsNotNull(allMetadata, "Metadata is not null");

                    var keyValuePairs = allMetadata.KeyValuePairs;
                    Assert.IsTrue(keyValuePairs.Count > 1, "KeyValuePairs contain at least 1 entry");
                    Assert.IsTrue(keyValuePairs.ContainsKey(CollectionNameConstants.DoubleCollectionName), "KeyValuePairs contain Key for DoubleCollection");
                    Assert.IsTrue(keyValuePairs[CollectionNameConstants.DoubleCollectionName] == "2.0.0", "Value for Key=DoubleCollection is 2.0.0");
                }
            }
        }

        [TestMethod]
        public void Get_CurrentVersion_Test()
        {
            using (var _ = CreateDatabase(out var database))
            {
                using (database)
                {
                    // Arrange
                    var data1 = new DoubleValueData(Guid.NewGuid(), new List<Guid>() { Guid.NewGuid() }, 1.234567);
                    var expectedVersion = new DbCollectionVersion(2, 0, 0);

                    // Act
                    database.Create(data1);
                    var currentVersion = database.GetCurrentVersion(data1.Id);

                    // Assert
                    Assert.IsNotNull(currentVersion, "Current version is not null");
                    DbCollectionVersionTests.CompareIVersion(expectedVersion, currentVersion);
                }
            }
        }

        [TestMethod]
        public void Get_SavedVersion_Test()
        {
            //Arrange
            using (var ms = CreateDatabase(out var newDatabase))
            {
                var guid = Guid.NewGuid();

                using (newDatabase)
                {
                    var data1 = new DoubleValueData(guid, new List<Guid>() { Guid.NewGuid() }, 1.234567);
                    newDatabase.Create(data1);
                }

                EditMetadata(
                    ms,
                    CollectionNameConstants.DoubleCollectionName,
                    "1.0.0");

                var expectedVersion = new DbCollectionVersion(1, 0, 0);
                
                //Act
                using (var savedDatabase = CreateDatabaseFromMemoryStream(ms))
                {
                    savedDatabase.ReadAll();
                    var savedVersion = savedDatabase.GetSavedVersion(guid);

                    // Assert
                    Assert.IsNotNull(savedVersion, "Saved version is not null");
                    DbCollectionVersionTests.CompareIVersion(expectedVersion, savedVersion);
                }
            }
        }

        [TestMethod]
        public void Check_Database_Can_Be_Converted_To_Bytes_And_Back()
        {
            using (var memoryStream = CreateDatabase(out var database))
            {
                using (database)
                {
                    // Arrange
                    var data1 = GetDoubleValueData();
                    var createSuccess1 = database.Create(data1);

                    // Act
                    database.Save(); // if we comment this line, allData.Count = 0 and readData = null
                    var databaseBytes = memoryStream.ToArray();
                    var newMemoryStream = new MemoryStream(databaseBytes);
                    using (var newDatabase = CreateDatabaseFromMemoryStream(newMemoryStream))
                    {
                        var allData = newDatabase.ReadAll();
                        var readData = newDatabase.Read(data1.Id);

                        // Assert
                        Assert.IsTrue(createSuccess1, "First data should create successfully");
                        Assert.AreEqual(allData.Count, 1,
                            "The number of data in database should be 1");
                        LiteDbCollectionAndQueryTests.CompareDoubleValueData(
                            data1, (DoubleValueData)readData);
                    }
                }
            }
        }

        private MemoryStream CreateDatabase(out IDatabase database)
        {
            // Use memory stream so no need to clear the file later
            // Only need to change to other database in future if any request
            // If they're database server, can change to return void
            var memoryStream = new MemoryStream();
            database = CreateDatabaseFromMemoryStream(memoryStream);
            return memoryStream;
        }

        private IDatabase CreateDatabaseFromMemoryStream(MemoryStream memoryStream)
        {
            return new LiteDbDatabase(memoryStream, AppDomain.CurrentDomain.GetAssemblies());
        }

        private DoubleValueData GetDoubleValueData()
        {
            var dataGuid = Guid.NewGuid();
            var parentsGuid = new List<Guid>() { Guid.NewGuid(), Guid.NewGuid() };
            const double dataValue = 1.234567;
            var doubleValueData = new DoubleValueData(dataGuid,
                parentsGuid,
                dataValue);

            return doubleValueData;
        }

        private void EditMetadata(MemoryStream ms, string collectionName, string collectionVersion)
        {
            using (var db = new LiteDatabase(ms))
            {
                var metadataCollection = db.GetCollection<DbComponentMetadata>(DbCollectionNameConstants.MetadataCollectionName);
                var metadata = metadataCollection.FindAll().First();
                metadata.KeyValuePairs[collectionName] = collectionVersion;
                metadataCollection.DeleteAll();
                metadataCollection.Insert(metadata);
            }
        }
    }
}
