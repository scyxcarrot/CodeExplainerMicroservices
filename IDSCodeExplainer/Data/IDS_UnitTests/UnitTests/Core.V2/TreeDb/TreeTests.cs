using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Model;
using IDS.Testing.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class TreeTests
    {
        [TestMethod]
        public void Create_Tree_From_Scratch_Test()
        {
            // Arrange
            /*
             *                            A(root)
             *
             */
            var testData = new OneToManyTestData();
            // Act
            var tree = new Tree(testData.A);
            // Assert
            Assert.IsNotNull(tree);
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
        }

        [TestMethod]
        public void Create_Tree_From_Database_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.ReadAll()).Returns(new List<IData>()
            {
                testData.A, 
                testData.B 
            });
            // Act
            var tree = new Tree(mockDb.Object);
            // Assert
            Assert.IsNotNull(tree);
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.C.Id), "Node C shouldn't exist in the tree");
        }

        [TestMethod]
        public void Add_Node_In_Tree_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            // Act
            var success = tree.AddNewNode(testData.B);
            // Assert
            Assert.IsTrue(success, "Node B should successfully added into the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree");
        }

        [TestMethod]
        public void Add_Same_Node_Twice_In_Tree_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            // Act
            var firstAdd = tree.AddNewNode(testData.B);
            var secondAdd = tree.AddNewNode(testData.B);
            // Assert
            Assert.IsTrue(firstAdd, "Node B should successfully added into the tree in first add");
            Assert.IsFalse(secondAdd, "Node B should failed to added into the tree in second add");
        }

        [TestMethod]
        public void Add_Node_Without_Parent_In_Tree_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *  <missing B>
             *      |
             *      D
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            // Act
            var success = tree.AddNewNode(testData.D);
            // Assert
            Assert.IsFalse(success, "Node D should failed to added into the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D shouldn't exist in the tree");
        }

        [TestMethod]
        public void Batch_Add_Node_In_Tree_Test()
        {
            // Arrange
            /*
             *                            A(root)
             *                            |
             *                -------------------------
             *                |                       |
             *                B                       C
             *                |                       |
             *      -----------------------       ---------
             *      |      |      |       |       |       |
             *      D      E      F       G       H       I
             *
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var batchData = testData.AllData;
            batchData.Remove(testData.A);
            // Act
            var success = tree.BatchAddNewNode(batchData.Cast<IData>().ToImmutableList());
            // Assert
            Assert.IsTrue(success, "All child nodes should successfully added into the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.C.Id), "Node C should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.D.Id), "Node D should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.E.Id), "Node E should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.F.Id), "Node F should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.G.Id), "Node G should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.H.Id), "Node H should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.I.Id), "Node I should exist in the tree");
        }

        [TestMethod]
        public void Batch_Add_Node_With_Missing_Node_Test()
        {
            // Arrange
            /*
             *                            A(root)
             *                            |
             *                -------------------------
             *                |                       |
             *                B                   <missing C>
             *                |                       |
             *      -----------------------       ---------
             *      |      |      |       |       |       |
             *      D      E      F       G       H       I
             *
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var batchData = testData.AllData;
            batchData.Remove(testData.A);
            batchData.Remove(testData.C);
            // Act
            var success = tree.BatchAddNewNode(batchData.Cast<IData>().ToImmutableList());
            // Assert
            Assert.IsFalse(success, "All child nodes should failed to added into the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.C.Id), "Node C shouldn't exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D shouldn't exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.E.Id), "Node E shouldn't exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.F.Id), "Node F shouldn't exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.G.Id), "Node G shouldn't exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.H.Id), "Node H shouldn't exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.I.Id), "Node I shouldn't exist in the tree");
        }

        [TestMethod]
        public void Remove_Node_In_Tree_Test()
        {
            // Arrange
            /*
             *                            A(root)
             *                            |
             *                -------------------------
             *                |                       |
             *                B                       C*
             *                |                       |
             *      -----------------------       ---------
             *      |      |      |       |       |       |
             *      D      E      F       G       H*      I*
             *
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var batchData = testData.AllData;
            batchData.Remove(testData.A);
            var batchAdded = tree.BatchAddNewNode(batchData.Cast<IData>().ToImmutableList());
            // Act
            var removedNodesId = tree.RemoveNode(testData.C.Id);
            // Assert
            Assert.IsTrue(batchAdded, "All child nodes should successfully added into the tree");
            
            Assert.AreEqual(3, removedNodesId.Count, "Should have 3 removed node ID");
            Assert.IsTrue(removedNodesId.Contains(testData.H.Id), "Node H should be removed");
            Assert.IsTrue(removedNodesId.Contains(testData.I.Id), "Node I should be removed");
            Assert.AreEqual(testData.C.Id, removedNodesId[2], "Node C should be removed at last");

            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.C.Id), "Node C shouldn't exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.D.Id), "Node D should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.E.Id), "Node E should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.F.Id), "Node F should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.G.Id), "Node G should exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.H.Id), "Node H shouldn't exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.I.Id), "Node I shouldn't exist in the tree");
        }

        [TestMethod]
        public void Remove_Node_Not_In_Tree_Test()
        {
            // Arrange
            /*
             *                  A(root)
             *                  |
             *                  B(not added)
             *
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            // Act
            var removedNodesId = tree.RemoveNode(testData.B.Id);
            // Assert
            Assert.IsNull(removedNodesId, "It should return null since the node is not exist in the tree");
        }

        // Other exception in the `RemoveNode` still can't figure out how to test it
    }
}
