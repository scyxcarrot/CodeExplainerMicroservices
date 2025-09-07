using IDS.Core.V2.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class IConnectionExtensionTests
    {
        [TestMethod]
        public void TestItemShouldBeRemoveIfConditionMeet()
        {
            // Act
            var list = new List<int>();
            for (var i = 0; i < 100; i++)
            {
                list.Add(i);
            }

            // Arrange
            list.RemoveIf(i => i % 2 == 0);
            
            // Act
            Assert.IsFalse(list.Any(i => i % 2 == 0), "Some value is still keep in list");
        }

        [TestMethod]
        public void TestItemShouldBeRemainIfConditionNotMeet()
        {
            // Act
            var list = new List<int>();
            for (var i = 0; i < 100; i++)
            {
                list.Add(i);
            }

            // Arrange
            var expectedRemainingItems = list.Where(i => i % 2 != 0).ToList();
            list.RemoveIf(i => i % 2 == 0);

            // Act
            Assert.IsTrue(expectedRemainingItems.Count == list.Count, "Remaining list is not equal count");
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreEqual(expectedRemainingItems[i], list[i]);
            }
        }
    }
}
