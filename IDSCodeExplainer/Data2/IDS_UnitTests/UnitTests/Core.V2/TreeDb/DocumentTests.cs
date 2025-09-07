using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Model;
using IDS.Testing.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests.V2
{
    // Document is integrate all the component, so the it not going to test with a lot of scenario,
    // but just simple one to verify the integration was implemented correctly
    [TestClass]
    public class DocumentTests
    {
        [TestMethod]
        public void Document_Create_From_Database_Test()
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
            var document = new IDSDocument(new TestConsole(), mockDb.Object);
            // Assert
            Assert.IsNotNull(document);

            mockDb.Verify(mock => mock.ReadAll(), Times.Once());

            var tree = TestUtilities.GetPrivateMember<IDSDocument, Tree>(document, "_tree");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree");
        }

        [TestMethod]
        public void Document_Create_From_Scratch_Test()
        {
            // Arrange
            /*
             *      A(root)
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            // Act
            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);
            // Assert
            Assert.IsNotNull(document);

            var tree = TestUtilities.GetPrivateMember<IDSDocument, Tree>(document, "_tree");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
        }

        [TestMethod]
        public void Document_Begin_Transaction_Test()
        {
            // Arrange
            /*
             *      A(root)
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);
            // Act
            var isSuccess = document.BeginTransaction();
            // Assert
            Assert.IsTrue(isSuccess, "Begin transaction should be success");
        }

        [TestMethod]
        public void Document_Begin_Transaction_Twice_Test()
        {
            // Arrange
            /*
             *      A(root)
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);
            // Act
            var first = document.BeginTransaction();
            var second = document.BeginTransaction();
            // Assert
            Assert.IsTrue(first, "Begin transaction should be success at first call");
            Assert.IsFalse(second, "Begin transaction should be failed at second call");
        }

        [TestMethod]
        public void Document_Create_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);

            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);
            // Act
            var isSuccess = document.Create(testData.B);
            // Assert
            Assert.IsTrue(isSuccess, "Document create data should be success");

            var tree = TestUtilities.GetPrivateMember<IDSDocument, Tree>(document, "_tree");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
        }

        [TestMethod]
        public void Document_BatchCreate_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             *      |
             *      D
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);

            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);
            // Act
            var isSuccess = document.BatchCreate(new List<IData>()
            {
                testData.B,
                testData.D
            });
            // Assert
            Assert.IsTrue(isSuccess, "Document create data should be success");
             
            var tree = TestUtilities.GetPrivateMember<IDSDocument, Tree>(document, "_tree");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree");
            Assert.IsTrue(tree.IsNodeExist(testData.D.Id), "Node D should exist in the tree");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Exactly(2));
        }

        [TestMethod]
        public void Document_Delete_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B*
             *      |
             *      D*
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.ReadAll()).Returns(new List<IData>()
            {
                testData.A,
                testData.B,
                testData.D
            });
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var document = new IDSDocument(new TestConsole(), mockDb.Object);

            var tree = TestUtilities.GetPrivateMember<IDSDocument, Tree>(document, "_tree");
            var isNodeAExist = tree.IsNodeExist(testData.A.Id);
            var isNodeBExist = tree.IsNodeExist(testData.B.Id);
            var isNodeDExist = tree.IsNodeExist(testData.D.Id);
            // Act
            var isSuccess = document.Delete(testData.B.Id);
            // Assert
            Assert.IsTrue(isNodeAExist, "Node A should added into the tree before delete");
            Assert.IsTrue(isNodeBExist, "Node B should added into the tree before delete");
            Assert.IsTrue(isNodeDExist, "Node D should added into the tree before delete");

            Assert.IsTrue(isSuccess, "Document create should be success");
            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after delete");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree after delete");
            Assert.IsFalse(tree.IsNodeExist(testData.D.Id), "Node D shouldn't exist in the tree after delete");

            mockDb.Verify(mock => mock.ReadAll(), Times.Once());
            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Exactly(2));
        }

        [TestMethod]
        public void Document_Undo_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();            
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);

            var isSuccess = document.Create(testData.B);

            var tree = TestUtilities.GetPrivateMember<IDSDocument, Tree>(document, "_tree");
            var isNodeAExist = tree.IsNodeExist(testData.A.Id);
            var isNodeBExist = tree.IsNodeExist(testData.B.Id);
            // Act
            document.Undo();
            // Assert

            Assert.IsTrue(isSuccess, "Document create data should be success");

            Assert.IsTrue(isNodeAExist, "Node A should exist in the tree before undo");
            Assert.IsTrue(isNodeBExist, "Node B should exist in the tree before undo");

            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after undo");
            Assert.IsFalse(tree.IsNodeExist(testData.B.Id), "Node B shouldn't exist in the tree after undo");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Once());
        }

        [TestMethod]
        public void Document_Redo_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);

            var isSuccess = document.Create(testData.B);
            document.Undo();

            var tree = TestUtilities.GetPrivateMember<IDSDocument, Tree>(document, "_tree");
            var isNodeAExist = tree.IsNodeExist(testData.A.Id);
            var isNodeBExist = tree.IsNodeExist(testData.B.Id);
            // Act
            document.Redo();
            // Assert

            Assert.IsTrue(isSuccess, "Document create data should be success");

            Assert.IsTrue(isNodeAExist, "Node A should exist in the tree before redo");
            Assert.IsFalse(isNodeBExist, "Node B shouldn't exist in the tree before redo");

            Assert.IsTrue(tree.IsNodeExist(testData.A.Id), "Node A should exist in the tree after redo");
            Assert.IsTrue(tree.IsNodeExist(testData.B.Id), "Node B should exist in the tree after redo");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Exactly(2));
            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Once());
        }

        [TestMethod]
        public void Document_Commit_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);

            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);

            var beginTransaction = document.BeginTransaction();
            var create = document.Create(testData.B);
            // Act
            var commit = document.Commit();
            // Assert
            Assert.IsTrue(beginTransaction, "Begin transaction should be success");
            Assert.IsTrue(create, "Create should be success");
            Assert.IsTrue(commit, "Commit should be success");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
        }

        [TestMethod]
        public void Document_Commit_Twice_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);

            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);

            var beginTransaction = document.BeginTransaction();
            var create = document.Create(testData.B);
            // Act
            var firstCommit = document.Commit();
            var secondCommit = document.Commit();
            // Assert
            Assert.IsTrue(beginTransaction, "Begin transaction should be success");
            Assert.IsTrue(create, "Create should be success");
            Assert.IsTrue(firstCommit, "First commit should be success");
            Assert.IsFalse(secondCommit, "Second commit should be failed");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
        }

        [TestMethod]
        public void Document_Commit_Without_Begin_Transaction_Test()
        {
            // Arrange
            /*
             *      A(root)
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);
            // Act
            var commit = document.Commit();
            // Assert
            Assert.IsFalse(commit, "Commit should be failed");
        }

        [TestMethod]
        public void Document_Rollback_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             *      |
             *      S
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);

            var beginTransaction = document.BeginTransaction();
            var firstCreate = document.Create(testData.B);
            var secondCreate = document.Create(testData.D);
            // Act
            var rollback = document.Rollback();
            // Assert
            Assert.IsTrue(beginTransaction, "Begin transaction should be success");
            Assert.IsTrue(firstCreate, "First create should be success");
            Assert.IsTrue(secondCreate, "Second create should be success");
            Assert.IsTrue(rollback, "Rollback should be success");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Exactly(2));
            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Exactly(2));
        }


        [TestMethod]
        public void Document_Rollback_Twice_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             *      |
             *      S
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });

            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);

            var beginTransaction = document.BeginTransaction();
            var create = document.Create(testData.B);
            // Act
            var firstRollback = document.Rollback();
            var secondRollback = document.Rollback();
            // Assert
            Assert.IsTrue(beginTransaction, "Begin transaction should be success");
            Assert.IsTrue(create, "Create should be success");
            Assert.IsTrue(firstRollback, "First rollback should be success");
            Assert.IsFalse(secondRollback, "Second rollback should be failed");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
        }

        [TestMethod]
        public void Document_Rollback_Without_Begin_Transaction_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             *      |
             *      S
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);

            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);

            var create = document.Create(testData.B);
            // Act
            var rollback = document.Rollback();
            // Assert
            Assert.IsTrue(create, "Create should be success");
            Assert.IsFalse(rollback, "Rollback should be failed");

            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Once());
        }

        [TestMethod]
        public void Document_Dispose_Test()
        {
            // Arrange
            /*
             *      A(root)
             *      |
             *      B
             */
            var testData = new OneToManyTestData();
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Dispose());
            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);
            // Act
            document.Dispose();
            // Assert
            mockDb.Verify(mock => mock.Dispose(), Times.Once());
        }

        [TestMethod]
        public void Document_MultiCommand_Undo_Test()
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
            var mockDb = new Mock<IDatabase>();
            mockDb.Setup(mock => mock.Delete(It.IsAny<Guid>()))
                .Returns((Guid id) => { return testData.AllData.FirstOrDefault(d => id == d.Id); });
            mockDb.Setup(mock => mock.Create(It.IsAny<IData>())).Returns(true);

            var document = new IDSDocument(new TestConsole(), mockDb.Object, testData.A);
            var batchCreate = document.BatchCreate(new List<IData>()
            {
                testData.B,
                testData.C,
                testData.D,
                testData.E,
                testData.F,
                testData.G
            });

            var beginTransaction = document.BeginTransaction();
            var delete = document.Delete(testData.B.Id);
            var create = document.Create(testData.H);
            var commit = document.Commit();

            // Act
            document.Undo();

            // Assert
            Assert.IsTrue(batchCreate, "BatchCreate should be success"); 
            Assert.IsTrue(beginTransaction, "Begin transaction should be success");
            Assert.IsTrue(delete, "Delete should be success");
            Assert.IsTrue(create, "Create should be success");
            Assert.IsTrue(commit, "Commit should be success");

            mockDb.Verify(mock => mock.Delete(It.IsAny<Guid>()), Times.Exactly(6));
            mockDb.Verify(mock => mock.Create(It.IsAny<IData>()), Times.Exactly(12));
        }
    }
}
