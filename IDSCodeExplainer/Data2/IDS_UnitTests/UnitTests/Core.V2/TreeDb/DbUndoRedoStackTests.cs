using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IDS.Testing.UnitTests.V2
{
    [TestClass]
    public class DbUndoRedoStackTests
    {
        private Mock<IDbCommand> CreateMockCommand()
        {
            var mockDbCommand = new Mock<IDbCommand>();
            mockDbCommand.Setup(mock => mock.Execute()).Returns(true);
            mockDbCommand.Setup(mock => mock.Undo());
            return mockDbCommand;
        }

        [TestMethod]
        public void Run_Db_Command_Test()
        {
            // Arrange
            var mockCommand = CreateMockCommand();
            var undoRedoStack = new DbUndoRedoStack();
            // Act
            var runSuccess = undoRedoStack.Run(mockCommand.Object);
            // Assert
            Assert.IsTrue(runSuccess, "Should run successfully");
            mockCommand.Verify(mock => mock.Execute(), Times.Once());
            mockCommand.Verify(mock => mock.Undo(), Times.Never());
        }

        [TestMethod]
        public void Undo_Db_Command_Test()
        {
            // Arrange
            var mockCommand = CreateMockCommand();
            var undoRedoStack = new DbUndoRedoStack();
            // Act
            var runSuccess = undoRedoStack.Run(mockCommand.Object);
            undoRedoStack.Undo();
            // Assert
            Assert.IsTrue(runSuccess, "Should run successfully");
            mockCommand.Verify(mock => mock.Execute(), Times.Once());
            mockCommand.Verify(mock => mock.Undo(), Times.Once());
        }

        [TestMethod]
        public void Redo_Db_Command_Test()
        {
            // Arrange
            var mockCommand = CreateMockCommand();
            var undoRedoStack = new DbUndoRedoStack();
            // Act
            var runSuccess = undoRedoStack.Run(mockCommand.Object);
            undoRedoStack.Undo();
            undoRedoStack.Redo();
            // Assert
            Assert.IsTrue(runSuccess, "Should run successfully");
            mockCommand.Verify(mock => mock.Execute(), Times.Exactly(2));
            mockCommand.Verify(mock => mock.Undo(), Times.Once());
        }

        [TestMethod]
        public void Multi_Begin_Transaction_Test()
        {
            // Arrange
            var undoRedoStack = new DbUndoRedoStack();
            // Act
            var first = undoRedoStack.BeginTransaction();
            var second = undoRedoStack.BeginTransaction();
            // Assert
            Assert.IsTrue(first, "First begin transaction should return true");
            Assert.IsFalse(second, "Second begin transaction should return false");
        }

        [TestMethod]
        public void Commit_Without_Begin_Transaction_Test()
        {
            // Arrange
            var undoRedoStack = new DbUndoRedoStack();
            // Act
            var isSuccess = undoRedoStack.Commit();
            // Assert
            Assert.IsFalse(isSuccess, "Commit should failed if without calling begin transaction");
        }

        [TestMethod]
        public void Rollback_Without_Begin_Transaction_Test()
        {
            // Arrange
            var undoRedoStack = new DbUndoRedoStack();
            // Act
            var isSuccess = undoRedoStack.Rollback();
            // Assert
            Assert.IsFalse(isSuccess, "Rollback should failed if without calling begin transaction");
        }

        [TestMethod]
        public void Rollback_Test()
        {
            // Arrange
            var mockCommand = CreateMockCommand();
            var undoRedoStack = new DbUndoRedoStack();
            // Act
            var successBeginTransaction = undoRedoStack.BeginTransaction();
            var successRun = undoRedoStack.Run(mockCommand.Object);
            var successRollback = undoRedoStack.Rollback();
            // Assert
            Assert.IsTrue(successBeginTransaction, "Begin transaction should success");
            Assert.IsTrue(successRun, "Run should success");
            Assert.IsTrue(successRollback, "Rollback should success");

            mockCommand.Verify(mock => mock.Execute(), Times.Once());
            mockCommand.Verify(mock => mock.Undo(), Times.Once());
        }

        [TestMethod]
        public void Commit_Test()
        {
            // Arrange
            var mockCommand = CreateMockCommand();
            var undoRedoStack = new DbUndoRedoStack();
            // Act
            var successBeginTransaction = undoRedoStack.BeginTransaction();
            var successRun = undoRedoStack.Run(mockCommand.Object);
            var successCommit = undoRedoStack.Commit();
            var successRollback = undoRedoStack.Rollback();
            // Assert
            Assert.IsTrue(successBeginTransaction, "Begin transaction should success");
            Assert.IsTrue(successRun, "Run should success");
            Assert.IsTrue(successCommit, "Commit should success");
            Assert.IsFalse(successRollback, "Rollback should failed");

            mockCommand.Verify(mock => mock.Execute(), Times.Once());
            mockCommand.Verify(mock => mock.Undo(), Times.Never());
        }

        [TestMethod]
        public void Clear_History_Db_Command_Test()
        {
            // Arrange
            var mockCommand = CreateMockCommand();
            var undoRedoStack = new DbUndoRedoStack();
            // Act
            var runSuccess = undoRedoStack.Run(mockCommand.Object);
            undoRedoStack.ClearHistory();
            undoRedoStack.Undo();
            // Assert
            Assert.IsTrue(runSuccess, "Should run successfully");
            mockCommand.Verify(mock => mock.Execute(), Times.Once());
            mockCommand.Verify(mock => mock.Undo(), Times.Never());
        }
    }
}
