using IDS.Core.V2.Common;
using IDS.Core.V2.TreeDb.DbCommand;
using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Model;
using IDS.Testing.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class DbCommandTests
    {
        [TestMethod]
        public void Create_Command_Execute_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            var createCommand = new CreateCommand(new TestConsole(), tree, mockDb.Object, testData.B);
            // Act
            var executeSuccess = createCommand.Execute();
            // Assert
            Assert.IsTrue(executeSuccess, "It should execute successfully");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree");
            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
        }

        [TestMethod]
        public void Create_Command_Failed_To_Execute_Test()
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
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            var createCommand = new CreateCommand(new TestConsole(), tree, mockDb.Object, testData.D);
            // Act
            var executeSuccess = createCommand.Execute();
            // Assert
            Assert.IsFalse(executeSuccess, "It should failed to execute");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D shouldn't exist in the tree");
            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Never());
        }

        [TestMethod]
        public void Create_Command_Failed_To_Execute_Due_To_Database_Rejected_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(false);

            var createCommand = new CreateCommand(new TestConsole(), tree, mockDb.Object, testData.B);
            // Act
            var executeSuccess = createCommand.Execute();
            // Assert
            Assert.IsFalse(executeSuccess, "It should failed to execute");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree since database ");
            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
        }

        [TestMethod]
        public void Create_Command_Failed_To_Execute_Due_To_Database_Exception_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>()))
                .Returns((IData data) => throw new IDSExceptionV2("Dummy"));
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>())).Returns(testData.B);

            var createCommand = new CreateCommand(new TestConsole(), tree, mockDb.Object, testData.B);
            // Act
            var executeSuccess = createCommand.Execute();
            // Assert
            Assert.IsFalse(executeSuccess, "It should failed to execute");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree since database ");
            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
        }

        [TestMethod]
        public void Create_Command_Undo_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>())).Returns(testData.B);
            var createCommand = new CreateCommand(new TestConsole(), tree, mockDb.Object, testData.B);
            // Act
            var executeSuccess = createCommand.Execute();
            var addedNodeA = tree.IsNodeExist(testData.A.Id);
            var addedNodeB = tree.IsNodeExist(testData.B.Id);
            createCommand.Undo();
            // Assert
            Assert.IsTrue(executeSuccess, "It should execute successfully");
            Assert.IsTrue(addedNodeA, "Node A should added into the tree before undo");
            Assert.IsTrue(addedNodeB, "Node B should added into the tree before undo");

            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after undo");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree after undo");
            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
        }

        [TestMethod]
        public void Delete_Command_Execute_Test()
        {
            // Arrange
            /*
             *                            A(root)
             *                            |
             *                -------------------------
             *                |                       |
             *                B*                      C
             *                |                       |
             *      -----------------------       ---------
             *      |      |      |       |       |       |
             *      D*     E*     F*      G*      H       I
             *
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var batchData = testData.AllData;
            batchData.Remove(testData.A);
            tree.BatchAddNewNode(batchData.Cast<IData>().ToImmutableList());

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var deleteCommand = new DeleteCommand(new TestConsole(), tree, mockDb.Object, testData.B.Id);

            var addedNodeA = tree.IsNodeExist(testData.A.Id);
            var addedNodeB = tree.IsNodeExist(testData.B.Id);
            var addedNodeC = tree.IsNodeExist(testData.C.Id);
            var addedNodeD = tree.IsNodeExist(testData.D.Id);
            var addedNodeE = tree.IsNodeExist(testData.E.Id);
            var addedNodeF = tree.IsNodeExist(testData.F.Id);
            var addedNodeG = tree.IsNodeExist(testData.G.Id);
            var addedNodeH = tree.IsNodeExist(testData.H.Id);
            var addedNodeI = tree.IsNodeExist(testData.I.Id);
            // Act
            var executeSuccess = deleteCommand.Execute();
            // Assert
            Assert.IsTrue(addedNodeA, "Node A should added into the tree before execute");
            Assert.IsTrue(addedNodeB, "Node B should added into the tree before execute");
            Assert.IsTrue(addedNodeC, "Node C should added into the tree before execute");
            Assert.IsTrue(addedNodeD, "Node D should added into the tree before execute");
            Assert.IsTrue(addedNodeE, "Node E should added into the tree before execute");
            Assert.IsTrue(addedNodeF, "Node F should added into the tree before execute");
            Assert.IsTrue(addedNodeG, "Node G should added into the tree before execute");
            Assert.IsTrue(addedNodeH, "Node H should added into the tree before execute");
            Assert.IsTrue(addedNodeI, "Node I should added into the tree before execute");

            Assert.IsTrue(executeSuccess, "It should execute successfully");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.C.Id), "Node C should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.E.Id), "Node E shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.F.Id), "Node F shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.G.Id), "Node G shouldn't exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.H.Id), "Node H should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.I.Id), "Node I should exist in the tree after execute");

            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Exactly(5));
        }

        [TestMethod]
        public void Delete_Command_Failed_To_Execute_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *  <missing B>
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var deleteCommand = new DeleteCommand(new TestConsole(), tree, mockDb.Object, testData.B.Id);
            // Act
            var executeSuccess = deleteCommand.Execute();
            // Assert
            Assert.IsFalse(executeSuccess, "It should failed to execute");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree after execute");

            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Never());
        }

        [TestMethod]
        public void Delete_Command_Undo_Test()
        {
            // Arrange
            /*
             *                            A(root)
             *                            |
             *                -------------------------
             *                |                       |
             *                B*                      C
             *                |                       |
             *      -----------------------       ---------
             *      |      |      |       |       |       |
             *      D*     E*     F*      G*      H       I
             *
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var batchData = testData.AllData;
            batchData.Remove(testData.A);
            tree.BatchAddNewNode(batchData.Cast<IData>().ToImmutableList());

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<DummyData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var deleteCommand = new DeleteCommand(new TestConsole(), tree, mockDb.Object, testData.B.Id);
            var executeSuccess = deleteCommand.Execute();

            var executeDeleteCommandNodeA = tree.IsNodeExist(testData.A.Id);
            var executeDeleteCommandNodeB = tree.IsNodeExist(testData.B.Id);
            var executeDeleteCommandNodeC = tree.IsNodeExist(testData.C.Id);
            var executeDeleteCommandNodeD = tree.IsNodeExist(testData.D.Id);
            var executeDeleteCommandNodeE = tree.IsNodeExist(testData.E.Id);
            var executeDeleteCommandNodeF = tree.IsNodeExist(testData.F.Id);
            var executeDeleteCommandNodeG = tree.IsNodeExist(testData.G.Id);
            var executeDeleteCommandNodeH = tree.IsNodeExist(testData.H.Id);
            var executeDeleteCommandNodeI = tree.IsNodeExist(testData.I.Id);
            // Act
            deleteCommand.Undo();
            // Assert
            Assert.IsTrue(executeSuccess, "It should execute successfully");
            Assert.IsTrue(executeDeleteCommandNodeA, "Node A should added into the tree before undo");
            Assert.IsFalse(executeDeleteCommandNodeB, "Node B shouldn't added into the tree before undo");
            Assert.IsTrue(executeDeleteCommandNodeC, "Node C should added into the tree before undo");
            Assert.IsFalse(executeDeleteCommandNodeD, "Node D shouldn't added into the tree before undo");
            Assert.IsFalse(executeDeleteCommandNodeE, "Node E shouldn't added into the tree before undo");
            Assert.IsFalse(executeDeleteCommandNodeF, "Node F shouldn't added into the tree before undo");
            Assert.IsFalse(executeDeleteCommandNodeG, "Node G shouldn't added into the tree before undo");
            Assert.IsTrue(executeDeleteCommandNodeH, "Node H should added into the tree before undo");
            Assert.IsTrue(executeDeleteCommandNodeI, "Node I should added into the tree before undo");

            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after undo");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree after undo");
            Assert.IsTrue(tree.IsNodeExist(testData.C.Id), "Node C should exist in the tree after undo");
            Assert.IsTrue(tree.IsNodeExist(testData.D.Id), "Node D should exist in the tree after undo");
            Assert.IsTrue(tree.IsNodeExist(testData.E.Id), "Node E should exist in the tree after undo");
            Assert.IsTrue(tree.IsNodeExist(testData.F.Id), "Node F should exist in the tree after undo");
            Assert.IsTrue(tree.IsNodeExist(testData.G.Id), "Node G should exist in the tree after undo");
            Assert.IsTrue(tree.IsNodeExist(testData.H.Id), "Node H should exist in the tree after undo");
            Assert.IsTrue(tree.IsNodeExist(testData.I.Id), "Node I should exist in the tree after undo");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Exactly(5));
            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Exactly(5));
        }

        [TestMethod]
        public void Batch_Create_Command_Execute_Test()
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

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var isNodeAExist = tree.IsNodeExist(testData.A.Id);
            var isNodeBExist = tree.IsNodeExist(testData.B.Id);
            var isNodeCExist = tree.IsNodeExist(testData.C.Id);
            var isNodeDExist = tree.IsNodeExist(testData.D.Id);
            var isNodeEExist = tree.IsNodeExist(testData.E.Id);
            var isNodeFExist = tree.IsNodeExist(testData.F.Id);
            var isNodeGExist = tree.IsNodeExist(testData.G.Id);
            var isNodeHExist = tree.IsNodeExist(testData.H.Id);
            var isNodeIExist = tree.IsNodeExist(testData.I.Id);

            var deleteCommand = new BatchCreateCommand(
                new TestConsole(),
                tree,
                mockDb.Object,
                batchData.Cast<IData>().ToList());

            // Act
            var executeSuccess = deleteCommand.Execute();
            // Assert
            Assert.IsTrue(isNodeAExist, "Node A should added into the tree before execute");
            Assert.IsFalse(isNodeBExist, "Node B shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeCExist, "Node C shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeDExist, "Node D shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeEExist, "Node E shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeFExist, "Node F shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeGExist, "Node G shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeHExist, "Node H shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeIExist, "Node I shouldn't added into the tree before execute");

            Assert.IsTrue(executeSuccess, "It should execute successfully");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.C.Id), "Node C should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.D.Id), "Node D should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.E.Id), "Node E should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.F.Id), "Node F should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.G.Id), "Node G should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.H.Id), "Node H should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.I.Id), "Node I should exist in the tree after execute");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Exactly(8));
        }

        [TestMethod]
        public void Batch_Create_Command_Execute_Failed_Test()
        {
            // Arrange
            /*
             *                            A(root)
             *                            |
             *                -------------------------
             *                |                       |
             *                B                  <missing C>
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

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);

            var isNodeAExist = tree.IsNodeExist(testData.A.Id);
            var isNodeBExist = tree.IsNodeExist(testData.B.Id);
            var isNodeCExist = tree.IsNodeExist(testData.C.Id);
            var isNodeDExist = tree.IsNodeExist(testData.D.Id);
            var isNodeEExist = tree.IsNodeExist(testData.E.Id);
            var isNodeFExist = tree.IsNodeExist(testData.F.Id);
            var isNodeGExist = tree.IsNodeExist(testData.G.Id);
            var isNodeHExist = tree.IsNodeExist(testData.H.Id);
            var isNodeIExist = tree.IsNodeExist(testData.I.Id);

            var batchCreateCommand = new BatchCreateCommand(
                new TestConsole(),
                tree,
                mockDb.Object,
                batchData.Cast<IData>().ToList());

            // Act
            var executeSuccess = batchCreateCommand.Execute();
            // Assert
            Assert.IsTrue(isNodeAExist, "Node A should added into the tree before execute");
            Assert.IsFalse(isNodeBExist, "Node B shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeCExist, "Node C shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeDExist, "Node D shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeEExist, "Node E shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeFExist, "Node F shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeGExist, "Node G shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeHExist, "Node H shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeIExist, "Node I shouldn't added into the tree before execute");

            Assert.IsFalse(executeSuccess, "It should failed to execute");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.C.Id), "Node C shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.E.Id), "Node E shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.F.Id), "Node F shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.G.Id), "Node G shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.H.Id), "Node H shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.I.Id), "Node I shouldn't exist in the tree after execute");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Never());
        }

        [TestMethod]
        public void Batch_Create_Command_Execute_Failed_Due_To_Database_Rejected_Test()
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

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(false);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => null);

            var isNodeAExist = tree.IsNodeExist(testData.A.Id);
            var isNodeBExist = tree.IsNodeExist(testData.B.Id);
            var isNodeCExist = tree.IsNodeExist(testData.C.Id);
            var isNodeDExist = tree.IsNodeExist(testData.D.Id);
            var isNodeEExist = tree.IsNodeExist(testData.E.Id);
            var isNodeFExist = tree.IsNodeExist(testData.F.Id);
            var isNodeGExist = tree.IsNodeExist(testData.G.Id);
            var isNodeHExist = tree.IsNodeExist(testData.H.Id);
            var isNodeIExist = tree.IsNodeExist(testData.I.Id);

            var batchCreateCommand = new BatchCreateCommand(
                new TestConsole(),
                tree,
                mockDb.Object,
                batchData.Cast<IData>().ToList());

            // Act
            var executeSuccess = batchCreateCommand.Execute();
            // Assert
            Assert.IsTrue(isNodeAExist, "Node A should added into the tree before execute");
            Assert.IsFalse(isNodeBExist, "Node B shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeCExist, "Node C shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeDExist, "Node D shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeEExist, "Node E shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeFExist, "Node F shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeGExist, "Node G shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeHExist, "Node H shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeIExist, "Node I shouldn't added into the tree before execute");

            Assert.IsFalse(executeSuccess, "It should failed to execute");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.C.Id), "Node C shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.E.Id), "Node E shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.F.Id), "Node F shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.G.Id), "Node G shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.H.Id), "Node H shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.I.Id), "Node I shouldn't exist in the tree after execute");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Exactly(8));
        }

        [TestMethod]
        public void Batch_Create_Command_Execute_Failed_Due_To_Database_Exception_Test()
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

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>()))
                .Returns((IData data) => throw new IDSExceptionV2("Dummy"));
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => null);

            var isNodeAExist = tree.IsNodeExist(testData.A.Id);
            var isNodeBExist = tree.IsNodeExist(testData.B.Id);
            var isNodeCExist = tree.IsNodeExist(testData.C.Id);
            var isNodeDExist = tree.IsNodeExist(testData.D.Id);
            var isNodeEExist = tree.IsNodeExist(testData.E.Id);
            var isNodeFExist = tree.IsNodeExist(testData.F.Id);
            var isNodeGExist = tree.IsNodeExist(testData.G.Id);
            var isNodeHExist = tree.IsNodeExist(testData.H.Id);
            var isNodeIExist = tree.IsNodeExist(testData.I.Id);

            var batchCreateCommand = new BatchCreateCommand(
                new TestConsole(),
                tree,
                mockDb.Object,
                batchData.Cast<IData>().ToList());

            // Act
            var executeSuccess = batchCreateCommand.Execute();
            // Assert
            Assert.IsTrue(isNodeAExist, "Node A should added into the tree before execute");
            Assert.IsFalse(isNodeBExist, "Node B shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeCExist, "Node C shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeDExist, "Node D shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeEExist, "Node E shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeFExist, "Node F shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeGExist, "Node G shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeHExist, "Node H shouldn't added into the tree before execute");
            Assert.IsFalse(isNodeIExist, "Node I shouldn't added into the tree before execute");

            Assert.IsFalse(executeSuccess, "It should failed to execute");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.C.Id), "Node C shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.E.Id), "Node E shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.F.Id), "Node F shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.G.Id), "Node G shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.H.Id), "Node H shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.I.Id), "Node I shouldn't exist in the tree after execute");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Exactly(8));
        }

        [TestMethod]
        public void Batch_Create_Command_Undo_Test()
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

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var batchCreateCommand = new BatchCreateCommand(
                new TestConsole(),
                tree,
                mockDb.Object,
                batchData.Cast<IData>().ToList());
            var executeSuccess = batchCreateCommand.Execute();

            var isNodeAExist = tree.IsNodeExist(testData.A.Id);
            var isNodeBExist = tree.IsNodeExist(testData.B.Id);
            var isNodeCExist = tree.IsNodeExist(testData.C.Id);
            var isNodeDExist = tree.IsNodeExist(testData.D.Id);
            var isNodeEExist = tree.IsNodeExist(testData.E.Id);
            var isNodeFExist = tree.IsNodeExist(testData.F.Id);
            var isNodeGExist = tree.IsNodeExist(testData.G.Id);
            var isNodeHExist = tree.IsNodeExist(testData.H.Id);
            var isNodeIExist = tree.IsNodeExist(testData.I.Id);
            // Act
            batchCreateCommand.Undo();
            // Assert
            Assert.IsTrue(isNodeAExist, "Node A should added into the tree before undo");
            Assert.IsTrue(isNodeBExist, "Node B should added into the tree before undo");
            Assert.IsTrue(isNodeCExist, "Node C should added into the tree before undo");
            Assert.IsTrue(isNodeDExist, "Node D should added into the tree before undo");
            Assert.IsTrue(isNodeEExist, "Node E should added into the tree before undo");
            Assert.IsTrue(isNodeFExist, "Node F should added into the tree before undo");
            Assert.IsTrue(isNodeGExist, "Node G should added into the tree before undo");
            Assert.IsTrue(isNodeHExist, "Node H should added into the tree before undo");
            Assert.IsTrue(isNodeIExist, "Node I should added into the tree before undo");

            Assert.IsTrue(executeSuccess, "It should execute successfully");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after undo");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.C.Id), "Node C should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.E.Id), "Node E should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.F.Id), "Node F should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.G.Id), "Node G should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.H.Id), "Node H should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.I.Id), "Node I should exist in the tree after execute");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Exactly(8));
            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Exactly(8));
        }

        [TestMethod]
        public void Multi_Command_Execute_Test()
        {
            // Arrange
            /*
             *                            A(root)
             *                            |
             *                -------------------------
             *                |                       |
             *                B*                      C
             *                |                       |
             *      -----------------------       ---------
             *      |      |      |       |       |       |
             *      D*     E*     F*      G*      H       I
             *
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var batchData = testData.AllData;
            batchData.Remove(testData.A);
            batchData.Remove(testData.H);
            batchData.Remove(testData.I);
            tree.BatchAddNewNode(batchData.Cast<IData>().ToImmutableList());

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var deleteCommand = new DeleteCommand(new TestConsole(), tree, mockDb.Object, testData.B.Id); 
            var createCommand = new CreateCommand(new TestConsole(), tree, mockDb.Object, testData.H);
            var multiCommand = new MultiCommand(new List<IDbCommand>
            {
                deleteCommand,
                createCommand
            });

            var addedNodeA = tree.IsNodeExist(testData.A.Id);
            var addedNodeB = tree.IsNodeExist(testData.B.Id);
            var addedNodeC = tree.IsNodeExist(testData.C.Id);
            var addedNodeD = tree.IsNodeExist(testData.D.Id);
            var addedNodeE = tree.IsNodeExist(testData.E.Id);
            var addedNodeF = tree.IsNodeExist(testData.F.Id);
            var addedNodeG = tree.IsNodeExist(testData.G.Id);
            var addedNodeH = tree.IsNodeExist(testData.H.Id);
            var addedNodeI = tree.IsNodeExist(testData.I.Id);

            // Act
            var executeSuccess = multiCommand.Execute();

            // Assert
            Assert.IsTrue(executeSuccess, "It should execute successfully");

            Assert.IsTrue(addedNodeA, "Node A should added into the tree before execute");
            Assert.IsTrue(addedNodeB, "Node B should added into the tree before execute");
            Assert.IsTrue(addedNodeC, "Node C should added into the tree before execute");
            Assert.IsTrue(addedNodeD, "Node D should added into the tree before execute");
            Assert.IsTrue(addedNodeE, "Node E should added into the tree before execute");
            Assert.IsTrue(addedNodeF, "Node F should added into the tree before execute");
            Assert.IsTrue(addedNodeG, "Node G should added into the tree before execute");
            Assert.IsFalse(addedNodeH, "Node H shouldn't added into the tree before execute");
            Assert.IsFalse(addedNodeI, "Node I shouldn't added into the tree before execute");

            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.C.Id), "Node C should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.E.Id), "Node E shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.F.Id), "Node F shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.G.Id), "Node G shouldn't exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.H.Id), "Node H should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.I.Id), "Node I shouldn't exist in the tree after execute");

            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Exactly(5));
            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once);
        }

        [TestMethod]
        public void Multi_Command_Failed_To_Execute_Test()
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

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var createCommand = new CreateCommand(new TestConsole(), tree, mockDb.Object, testData.D);
            var deleteCommand = new DeleteCommand(new TestConsole(), tree, mockDb.Object, testData.C.Id);
            var multiCommand = new MultiCommand(new List<IDbCommand>
            {
                createCommand,
                deleteCommand
            });

            // Act
            var executeSuccess = multiCommand.Execute();

            // Assert
            Assert.IsFalse(executeSuccess, "It should failed to execute");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D shouldn't exist in the tree");
            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Never());
            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Never());
        }

        [TestMethod]
        public void Multi_Command_Undo_Test()
        {
            // Arrange
            /*
             *                            A(root)
             *                            |
             *                -------------------------
             *                |                       |
             *                B*                      C
             *                |                       |
             *      -----------------------       ---------
             *      |      |      |       |       |       |
             *      D*     E*     F*      G*      H       I
             *
             */
            var testData = new OneToManyTestData();
            var tree = new Tree(testData.A);
            var batchData = testData.AllData;
            batchData.Remove(testData.A);
            batchData.Remove(testData.H);
            batchData.Remove(testData.I);
            tree.BatchAddNewNode(batchData.Cast<IData>().ToImmutableList());

            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var deleteCommand = new DeleteCommand(new TestConsole(), tree, mockDb.Object, testData.B.Id);
            var createCommand = new CreateCommand(new TestConsole(), tree, mockDb.Object, testData.H);
            var multiCommand = new MultiCommand(new List<IDbCommand>
            {
                deleteCommand,
                createCommand
            });

            var executeSuccess = multiCommand.Execute();

            var addedNodeA = tree.IsNodeExist(testData.A.Id);
            var addedNodeB = tree.IsNodeExist(testData.B.Id);
            var addedNodeC = tree.IsNodeExist(testData.C.Id);
            var addedNodeD = tree.IsNodeExist(testData.D.Id);
            var addedNodeE = tree.IsNodeExist(testData.E.Id);
            var addedNodeF = tree.IsNodeExist(testData.F.Id);
            var addedNodeG = tree.IsNodeExist(testData.G.Id);
            var addedNodeH = tree.IsNodeExist(testData.H.Id);
            var addedNodeI = tree.IsNodeExist(testData.I.Id);

            // Act
            multiCommand.Undo();

            // Assert
            Assert.IsTrue(executeSuccess, "It should execute successfully");

            Assert.IsTrue(addedNodeA, "Node A should added into the tree before execute");
            Assert.IsFalse(addedNodeB, "Node B shouldn't added into the tree before execute");
            Assert.IsTrue(addedNodeC, "Node C should added into the tree before execute");
            Assert.IsFalse(addedNodeD, "Node D shouldn't added into the tree before execute");
            Assert.IsFalse(addedNodeE, "Node E shouldn't added into the tree before execute");
            Assert.IsFalse(addedNodeF, "Node F shouldn't added into the tree before execute");
            Assert.IsFalse(addedNodeG, "Node G shouldn't added into the tree before execute");
            Assert.IsTrue(addedNodeH, "Node H should added into the tree before execute");
            Assert.IsFalse(addedNodeI, "Node I shouldn't added into the tree before execute");

            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.C.Id), "Node C should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.D.Id), "Node D should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.E.Id), "Node E should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.F.Id), "Node F should exist in the tree after execute");
            Assert.IsTrue(tree.IsNodeExist(testData.G.Id), "Node G should exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.H.Id), "Node H shouldn't exist in the tree after execute");
            Assert.IsFalse(tree.IsNodeExist(testData.I.Id), "Node I shouldn't exist in the tree after execute");

            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Exactly(6));
            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Exactly(6));
        }
    }
}
