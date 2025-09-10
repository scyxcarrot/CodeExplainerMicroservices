using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using IDS.Core.V2.Databases;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class DatabaseNameTests
    {
        [TestMethod]
        public void Check_All_Collection_Names_Are_Unique()
        {
            // Arrange and act
            var collectionNames = typeof(DbCollectionNameConstants)
                .GetFields()
                .Select(fieldInfo => fieldInfo.GetValue(null).ToString())
                .ToList();

            // Assert
            Assert.IsTrue(collectionNames.Distinct().Count() == collectionNames.Count);
        }

        [TestMethod]
        public void Check_Total_Collection_Names_Can_Fit_Into_LiteDb_Header()
        {
            // The collection names are stored inside the header of lite db
            // The header has a file size limitation of 8192bytes, the full calculation will be listed here

            // Arrange
            var maxHeaderSize = 8192;
            var headInformation = 192;
            var startBytes = 4;
            var endBytes = 9;

            // Act
            var collectionNames = typeof(DbCollectionNameConstants)
                .GetFields()
                .Select(fieldInfo => fieldInfo.GetValue(null).ToString())
                .ToList();
            
            var totalStringLength = collectionNames
                .Sum(collectionName => collectionName.Length);
            // +6 bytes for each collection name
            var totalBytes = totalStringLength + collectionNames.Count * 6;

            // Assert
            // add everything up and if its less than maxHeaderSize, then pass
            var totalHeaderSize = headInformation + startBytes + endBytes + totalBytes;
            Assert.IsTrue(maxHeaderSize > totalHeaderSize,
                "The collection names cannot be all stored in the database, " +
                "please shorten the collection names or remove some collections");
        }
    }
}
