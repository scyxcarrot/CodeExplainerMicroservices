using IDS.Core.V2.TreeDb.DbCommand;
using IDS.Core.V2.TreeDb.Interface;
using System.Collections.Generic;

namespace IDS.Core.V2.TreeDb.Model
{
    public sealed class DbUndoRedoStack
    {
        private int _checkPoint = -1;
        private readonly List<IDbCommand> _undoCommandStack = new List<IDbCommand>();
        private readonly List<IDbCommand> _redoCommandStack = new List<IDbCommand>();

        public bool Run(IDbCommand command)
        {
            var result = command.Execute();
            if (result)
            {
                _undoCommandStack.Add(command);
                _redoCommandStack.Clear();
            }

            return result;
        }

        public bool BeginTransaction()
        {
            if (_checkPoint >= 0)
            {
                return false;
            }

            _checkPoint = _undoCommandStack.Count;
            return true;
        }

        public void Undo()
        {
            var lastIndex = _undoCommandStack.Count - 1;
            if (lastIndex < 0)
                return;

            var lastCommand = _undoCommandStack[lastIndex];
            if (lastCommand != null)
            {
                lastCommand.Undo();

                _undoCommandStack.RemoveAt(lastIndex);
                _redoCommandStack.Add(lastCommand);
            }
        }

        public void Redo()
        {
            var lastIndex = _redoCommandStack.Count - 1;
            if (lastIndex < 0)
                return;

            var lastCommand = _redoCommandStack[lastIndex];
            if (lastCommand != null)
            {
                lastCommand.Execute();

                _redoCommandStack.RemoveAt(lastIndex);
                _undoCommandStack.Add(lastCommand);
            }
        }

        public bool Commit()
        {
            if (_checkPoint < 0 ||
                _checkPoint > _undoCommandStack.Count)
            {
                return false;
            }

            var commandCount = _undoCommandStack.Count - _checkPoint;
            if (commandCount > 1)
            {
                var commandList = new List<IDbCommand>();
                for (var i = _checkPoint; i < _undoCommandStack.Count; i++)
                {
                    commandList.Add(_undoCommandStack[i]);
                }

                _undoCommandStack.RemoveRange(_checkPoint, commandCount);
                _undoCommandStack.Add(new MultiCommand(commandList));
            }

            _checkPoint = -1;
            return true;
        }

        public bool Rollback()
        {
            if (_checkPoint < 0 ||
                _checkPoint >= _undoCommandStack.Count)
            {
                return false;
            }

            while (_checkPoint < _undoCommandStack.Count)
            {
                Undo();
            }

            _redoCommandStack.Clear();
            _checkPoint = -1;
            return true;
        }

        public void ClearHistory()
        {
            _undoCommandStack.Clear();
            _redoCommandStack.Clear();
        }

        public int GetUndoStackCount()
        {
            return _undoCommandStack.Count;
        }
    }
}
