using IDS.CMFImplantCreation.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace IDS.CMFImplantCreation.UnitTests
{
    [TestClass]
    public class IntermediatePartUtilitiesTests
    {
        [TestMethod]
        public void FindNextUnusedKey_Returns_Given_Key_When_List_Is_Empty()
        {
            // Arrange
            var keys = new List<string>();
            var key = "Tube";

            // Act
            var nextUnusedKey = IntermediatePartUtilities.FindNextUnusedKey(keys, key);

            // Assert
            Assert.AreEqual(key, nextUnusedKey);
        }

        [TestMethod]
        public void FindNextUnusedKey_Returns_Given_Key_When_List_Does_Not_Contain_Key()
        {
            // Arrange
            var keys = new List<string>
            {
                "Box",
                "Square"
            };
            var key = "Tube";

            // Act
            var nextUnusedKey = IntermediatePartUtilities.FindNextUnusedKey(keys, key);

            // Assert
            Assert.AreEqual(key, nextUnusedKey);
        }

        [TestMethod]
        public void FindNextUnusedKey_Returns_Given_Key_When_List_Only_Contain_Key_With_PostFix()
        {
            // Arrange
            var keys = new List<string>
            {
                "Box",
                "Square",
                "Tube-10",
                "Tube-15"
            };
            var key = "Tube";

            // Act
            var nextUnusedKey = IntermediatePartUtilities.FindNextUnusedKey(keys, key);

            // Assert
            Assert.AreEqual(key, nextUnusedKey);
        }

        [TestMethod]
        public void FindNextUnusedKey_Returns_Given_Key_With_PostFix_When_List_Contain_Key()
        {
            // Arrange
            var keys = new List<string>
            {
                "Box",
                "Square",
                "Tube"
            };
            var key = "Tube";

            // Act
            var nextUnusedKey = IntermediatePartUtilities.FindNextUnusedKey(keys, key);

            // Assert
            Assert.AreEqual("Tube-1", nextUnusedKey);
        }

        [TestMethod]
        public void FindNextUnusedKey_Returns_Given_Key_With_Next_PostFix_When_List_Contain_Key()
        {
            // Arrange
            var keys = new List<string>
            {
                "Box",
                "Square",
                "Tube",
                "Tube-10"
            };
            var key = "Tube";

            // Act
            var nextUnusedKey = IntermediatePartUtilities.FindNextUnusedKey(keys, key);

            // Assert
            Assert.AreEqual("Tube-11", nextUnusedKey);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FindNextUnusedKey_Throws_Exception_When_Given_Key_Contain_Illegal_Character()
        {
            // Arrange
            var keys = new List<string>
            {
                "Box",
                "Square",
                "Tube",
                "Tube-1",
                "Tube-10"
            };
            var key = "Tube-1";

            // Act
            IntermediatePartUtilities.FindNextUnusedKey(keys, key);

            // Assert
            //Exception thrown
        }
    }
}