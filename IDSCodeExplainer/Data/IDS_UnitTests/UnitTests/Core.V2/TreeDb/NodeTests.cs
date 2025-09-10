using IDS.Core.V2.TreeDb.Model;
using IDS.Core.V2.TreeDb.Utilities;
using IDS.Testing.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class NodeTests
    {
        [TestMethod]
        public void Add_Node_Test()
        {
            // Arrange
            /*
             *                            A
             *                            |
             *                -------------------------
             *                |                       |
             *                B                       C
             *
             */
            var testData = new OneToManyTestData();
            var nodeA = new TreeNode(testData.A);
            var nodeB = new TreeNode(testData.B);
            var nodeC = new TreeNode(testData.C);

            // Act
            nodeA.AddNode(nodeB);
            nodeA.AddNode(nodeC);

            // Assert
            var childNode = nodeA.GetChildNodes();
            Assert.AreEqual(2, childNode.Count, "Node A should have 2 child node");
            Assert.IsTrue(childNode.Exists(child => child.Id == testData.B.Id),
                "Node A should have only child node B");
            Assert.IsTrue(childNode.Exists(child => child.Id == testData.C.Id),
                "Node A should have only child node C");

            childNode = nodeB.GetChildNodes();
            Assert.AreEqual(0, childNode.Count, "Node B shouldn't have child node");

            childNode = nodeC.GetChildNodes();
            Assert.AreEqual(0, childNode.Count, "Node B shouldn't have child node");
        }


        [TestMethod]
        public void Same_Node_Add_Multiple_Test()
        {
            // Arrange
            /*
             *    A
             *    |
             *    B
             *    |
             *    D x 2
             */
            var testData = new OneToManyTestData();
            var nodeA = new TreeNode(testData.A);
            var nodeB = new TreeNode(testData.B);
            var nodeD = new TreeNode(testData.D);

            // Act
            nodeA.AddNode(nodeB);
            nodeB.AddNode(nodeD);
            nodeB.AddNode(nodeD);//Added twice

            // Assert
            var childNode = nodeA.GetChildNodes(); 
            Assert.AreEqual(1, childNode.Count, "Node A should have only one child node");
            Assert.AreEqual(testData.B.Id, childNode[0].Id, "Node A should have only child node B");

            childNode = nodeB.GetChildNodes();
            Assert.AreEqual(1, childNode.Count, "Node B should have only one child node");
            Assert.AreEqual(testData.D.Id, childNode[0].Id, "Node B should have only child node D");

            childNode = nodeD.GetChildNodes();
            Assert.AreEqual(0, childNode.Count, "Node D shouldn't have child node");
        }

        [TestMethod]
        public void Add_Node_With_Circular_Dependency_Test()
        {
            // Arrange
            /*
             *               A
             *               |
             *           ---------
             *           |       |
             *      -----B       D
             *      |    |
             *      -----C
             */
            var testData = new CircularDependencyTestData();
            var nodeA = new TreeNode(testData.A);
            var nodeB = new TreeNode(testData.B);
            var nodeC = new TreeNode(testData.C);
            var nodeD = new TreeNode(testData.D);

            // Act
            nodeA.AddNode(nodeB);
            nodeA.AddNode(nodeD);
            nodeB.AddNode(nodeC);
            nodeC.AddNode(nodeB);

            // Assert
            var childNode = nodeA.GetChildNodes(); 
            Assert.AreEqual(2, childNode.Count, "Node A should have 2 child node");
            Assert.IsTrue(childNode.Exists(child => child.Id == testData.B.Id),
                "Node A should have only child node B");
            Assert.IsTrue(childNode.Exists(child => child.Id == testData.D.Id),
                "Node A should have only child node D");

            childNode = nodeB.GetChildNodes(); 
            Assert.AreEqual(1, childNode.Count, "Node B should have only one child node");
            Assert.AreEqual(testData.C.Id, childNode[0].Id, "Node B should have only child node C");

            childNode = nodeC.GetChildNodes(); 
            Assert.AreEqual(1, childNode.Count, "Node C should have only one child node");
            Assert.AreEqual(testData.B.Id, childNode[0].Id, "Node C should have only child node B");

            childNode = nodeD.GetChildNodes(); 
            Assert.AreEqual(0, childNode.Count, "Node D shouldn't have child node");
        }

        [TestMethod]
        public void Delete_Node_Test()
        {
            // Arrange
            /*
             *                            A
             *                            |
             *                -------------------------
             *                |                       |
             *                B                      *C
             *                |                       |
             *      -----------------------       ---------
             *      |      |      |       |       |       |
             *      D      E      F       G      *H      *I
             *
             */
            var testData = new OneToManyTestData();
            var nodeA = new TreeNode(testData.A);
            var nodeB = new TreeNode(testData.B);
            var nodeC = new TreeNode(testData.C);
            var nodeD = new TreeNode(testData.D);
            var nodeE = new TreeNode(testData.E);
            var nodeF = new TreeNode(testData.F);
            var nodeG = new TreeNode(testData.G);
            var nodeH = new TreeNode(testData.H);
            var nodeI = new TreeNode(testData.I);

            nodeA.AddNode(nodeB);
            nodeA.AddNode(nodeC);

            nodeB.AddNode(nodeD);
            nodeB.AddNode(nodeE);
            nodeB.AddNode(nodeF);
            nodeB.AddNode(nodeG);

            nodeC.AddNode(nodeH);
            nodeC.AddNode(nodeI);

            // Act
            var deletedNodesId = nodeC.CascadeDeleteFromTheNode();

            // Assert
            var childNode = nodeA.GetChildNodes();
            Assert.AreEqual(1, childNode.Count, "Node A should have only a child node");
            Assert.IsFalse(childNode.Contains(nodeC), "Node A shouldn't contain deleted child");

            Assert.AreEqual(3, deletedNodesId.Count, "It should have 3 deleted node");
            Assert.AreEqual(testData.H.Id, deletedNodesId[0], "H should be the 1st deleted node");
            Assert.AreEqual(testData.I.Id, deletedNodesId[1], "I should be the 2nd deleted node");
            Assert.AreEqual(testData.C.Id, deletedNodesId[2], "C should be the 3rd deleted node");
        }

        [TestMethod]
        public void Delete_Node_With_Circular_Dependency_Test()
        {
            // Arrange
            /*
             *               A
             *               |
             *           ---------
             *           |       |
             *      ----*B       D
             *      |    |
             *      ----*C
             */
            var testData = new CircularDependencyTestData();
            var nodeA = new TreeNode(testData.A);
            var nodeB = new TreeNode(testData.B);
            var nodeC = new TreeNode(testData.C);
            var nodeD = new TreeNode(testData.D);

            nodeA.AddNode(nodeB);
            nodeA.AddNode(nodeD);
            nodeB.AddNode(nodeC);
            nodeC.AddNode(nodeB);

            // Act
            var deletedNodesId = nodeB.CascadeDeleteFromTheNode();

            // Assert
            var childNode = nodeA.GetChildNodes();
            Assert.AreEqual(1, childNode.Count, "Node A should have only a child node");
            Assert.IsFalse(childNode.Contains(nodeB), "Node A shouldn't contain deleted child");

            Assert.AreEqual(2, deletedNodesId.Count, "It should have 2 deleted node");
            Assert.AreEqual(nodeC.Id, deletedNodesId[0], "C should be the 1st deleted node");
            Assert.AreEqual(nodeB.Id, deletedNodesId[1], "B should be the 2nd deleted node");
        }

        [TestMethod]
        public void Delete_One_To_Many_Test()
        {
            // Arrange
            /*
             *                            A
             *                            |
             *                -------------------------
             *                |                       |
             *               *B                       C
             *                |                       |
             *      -----------------------       ---------
             *      |      |      |       |       |       |
             *     *D     *E     *F      *G       H       I
             *
             */
            var testData = new OneToManyTestData();
            var nodes = TreeNodeUtilities.GetConnectedTreeNodes(testData.AllData);
            var nodeA = nodes.First(n => n.Id == testData.A.Id);
            var nodeB = nodes.First(n => n.Id == testData.B.Id);

            // Act
            var deletedNodesId = nodeB.CascadeDeleteFromTheNode();

            // Assert
            var childNode = nodeA.GetChildNodes();
            Assert.AreEqual(1, childNode.Count, "Node A should have only a child node");
            Assert.IsFalse(childNode.Contains(nodeB), "Node A shouldn't contain deleted child");

            Assert.AreEqual(5, deletedNodesId.Count, "It should have 5 deleted node");
            // Order is not important for the child layers
            Assert.IsTrue(deletedNodesId.Contains(testData.D.Id), "D should be deleted");
            Assert.IsTrue(deletedNodesId.Contains(testData.E.Id), "E should be deleted");
            Assert.IsTrue(deletedNodesId.Contains(testData.F.Id), "F should be deleted");
            Assert.IsTrue(deletedNodesId.Contains(testData.G.Id), "G should be deleted");
            // Important is the B should delete after all child deleted
            Assert.AreEqual(nodeB.Id, deletedNodesId[4], "B should be the last deleted node");
        }

        [TestMethod]
        public void Delete_Many_TO_One_Test()
        {
            // Arrange
            /*
             *
             *             A
             *             |
             *      ----------------
             *      |      |       |
             *     *B      C       D
             *       \    /  \    /
             *        \  /    \  /
             *         *E       F
             *
             */
            var testData = new ManyToOneTestData();
            var nodes = TreeNodeUtilities.GetConnectedTreeNodes(testData.AllData);
            var nodeA = nodes.First(n => n.Id == testData.A.Id);
            var nodeB = nodes.First(n => n.Id == testData.B.Id);

            // Act
            var deletedNodesId = nodeB.CascadeDeleteFromTheNode();

            // Assert
            var childNode = nodeA.GetChildNodes();
            Assert.AreEqual(2, childNode.Count, "Node A should have 2 child node");
            Assert.IsFalse(childNode.Contains(nodeB), "Node A shouldn't contain deleted child");

            Assert.AreEqual(2, deletedNodesId.Count, "It should have 2 deleted node");
            Assert.AreEqual(testData.E.Id, deletedNodesId[0], "E should be the 1st deleted node");
            Assert.AreEqual(testData.B.Id, deletedNodesId[1], "B should be the 2nd deleted node");
        }
    }
}
