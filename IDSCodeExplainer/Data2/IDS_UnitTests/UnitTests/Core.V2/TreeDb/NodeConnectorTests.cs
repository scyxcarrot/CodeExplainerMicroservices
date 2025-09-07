using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Immutable;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class NodeConnectorTests
    {
        private IData CreateDummyData()
        {
            var mockIData = new Mock<IData>();
            mockIData.SetupGet(m => m.Id).Returns(
                Guid.Empty);
            mockIData.SetupGet(m => m.Parents).Returns(
                ImmutableList.Create<Guid>());
            return mockIData.Object;
        }

        [TestMethod]
        public void Is_Valid_Missing_Test()
        {
            // Arrange
            var nodeA = new TreeNode(CreateDummyData());
            var nodeB = new TreeNode(CreateDummyData());

            var connector = new NodeConnector(nodeA, nodeB);
            // Act
            var isValid = connector.IsValid;
            // Assert
            Assert.IsTrue(isValid, "It should be valid since parent and child have instance");
        }

        [TestMethod]
        public void Is_Invalid_When_Parent_Missing_Test()
        {
            // Arrange
            var node = new TreeNode(CreateDummyData());
            var connector = new NodeConnector(node, null);
            // Act
            var isValid = connector.IsValid;
            // Assert
            Assert.IsFalse(isValid, "It should be invalid since child is null");
        }

        [TestMethod]
        public void Is_Invalid_When_Child_Missing_Test()
        {
            // Arrange
            var node = new TreeNode(CreateDummyData());
            var connector = new NodeConnector(null, node);
            // Act
            var isValid = connector.IsValid;
            // Assert
            Assert.IsFalse(isValid, "It should be invalid since parent is null");
        }

        [TestMethod]
        public void Is_Invalid_When_Both_Missing_Test()
        {
            // Arrange
            var connector = new NodeConnector(null, null);
            // Act
            var isValid = connector.IsValid;
            // Assert
            Assert.IsFalse(isValid, "It should be invalid since parent and child is null");
        }

        [TestMethod]
        public void Disconnect_Parent_Test()
        {
            // Arrange
            var node = new TreeNode(CreateDummyData());
            var connector = new NodeConnector(node, null);
            // Act
            var parentBeforeDisconnect = connector.Parent;
            connector.DisconnectFromParent();
            var parentAfterDisconnect = connector.Parent;
            // Assert
            Assert.IsNotNull(parentBeforeDisconnect, "Parent shouldn't be null before disconnect");
            Assert.IsNull(parentAfterDisconnect, "Parent should be null after disconnected");
        }

        [TestMethod]
        public void Disconnect_Child_Test()
        {
            // Arrange
            var node = new TreeNode(CreateDummyData());
            var connector = new NodeConnector(null, node);
            // Act
            var childBeforeDisconnect = connector.Child;
            connector.DisconnectFromChild();
            var childAfterDisconnect = connector.Child;
            // Assert
            Assert.IsNotNull(childBeforeDisconnect, "Child shouldn't be null before disconnect");
            Assert.IsNull(childAfterDisconnect, "Child should be null after disconnected");
        }
    }
}
