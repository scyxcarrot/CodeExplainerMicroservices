using IDS.Core.V2.TreeDb.DbCommand;
using IDS.Core.V2.TreeDb.Interface;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;

namespace IDS.Core.V2.TreeDb.Model
{
    public sealed class IDSDocument
    {
        private readonly DbUndoRedoStack _dbUndoRedoStack;
        private readonly IConsole _console;
        private readonly IDatabase _database;
        private readonly Tree _tree;

        private IDSDocument()
        {
            _dbUndoRedoStack = new DbUndoRedoStack();
        }


        /// <summary>
        /// Create from document load from database
        /// </summary>
        /// <param name="console">Console for the platform</param>
        /// <param name="database">Database with data</param>
        public IDSDocument(IConsole console, IDatabase database): this()
        {
            _console = console;
            _database = database;
            _tree = new Tree(_database);
        }

        /// <summary>
        /// Create from document scratch
        /// </summary>
        /// <param name="console">Console for the platform</param>
        /// <param name="database">Database from scratch</param>
        /// <param name="rootData">Root data</param>
        public IDSDocument(IConsole console, IDatabase database, IData rootData) : this()
        {
            _console = console;
            _database = database;
            _tree = new Tree(rootData);
        }

        public bool BeginTransaction()
        {
            lock (this)
            {
                return _dbUndoRedoStack.BeginTransaction();
            }
        }

        public bool Create(IData data)
        {
            lock (this)
            {
                var command = new CreateCommand(
                    _console,
                    _tree,
                    _database,
                    data);

                return _dbUndoRedoStack.Run(command);
            }
        }

        public bool BatchCreate(IList<IData> batchData)
        {
            lock (this)
            {
                var command = new BatchCreateCommand(
                    _console,
                    _tree,
                    _database,
                    batchData);

                return _dbUndoRedoStack.Run(command);
            }
        }

        public bool Delete(IEnumerable<Guid> ids)
        {
            var success = true;
            foreach (var id in ids)
            {
                success &= Delete(id);
            }

            return success;
        }

        public bool Delete(Guid id)
        {
            lock (this)
            {
                var command = new DeleteCommand(
                    _console,
                    _tree,
                    _database,
                    id);

                return _dbUndoRedoStack.Run(command);
            }
        }

        public bool AddEmptyCommand()
        {
            lock (this)
            {
                var command = new EmptyCommand(_console);

                return _dbUndoRedoStack.Run(command);
            }
        }

        public void Undo()
        {
            lock (this)
            {
                _dbUndoRedoStack.Undo();
            }
        }

        public void Redo()
        {
            lock (this)
            {
                _dbUndoRedoStack.Redo();
            }
        }

        public bool Commit()
        {
            lock (this)
            {
                return _dbUndoRedoStack.Commit();
            }
        }

        public bool Rollback()
        {
            lock (this)
            {
                return _dbUndoRedoStack.Rollback();
            }
        }

        public byte[] GetDatabaseBytes()
        {
            return _database.GetBytes();
        }

        public IData GetNode(Guid id)
        {
            return _database.Read(id);
        }

        public bool IsNodeInTree(Guid id)
        {
            var nodeInTree = _tree.IsNodeExist(id);
            var nodeInDatabase = _database.Read(id) != null;

            return nodeInTree && nodeInDatabase;
        }

        public List<Guid> GetChildrenInTree(Guid id)
        {
            if (!IsNodeInTree(id))
            {
                return null;
            }

            return _tree.GetChildrenNodeIds(id);
        }

        public void ClearUndoRedo()
        {
            lock (this)
            {
                _dbUndoRedoStack.ClearHistory();
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                _database.Dispose();
            }
        }

        public int GetUndoStackCount()
        {
            lock (this)
            {
                return _dbUndoRedoStack.GetUndoStackCount();
            }
        }
    }
}
