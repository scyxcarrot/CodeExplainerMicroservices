using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Model;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Core.V2.TreeDb.DbCommand
{
    public sealed class DeleteCommand : IDbCommand
    {
        private readonly IConsole _console;
        private readonly Tree _tree;
        private readonly IDatabase _database;
        private readonly Guid _id;
        private List<IData> _allDeletedData;

        public DeleteCommand(
            IConsole console,
            Tree tree,
            IDatabase database,
            Guid id)
        {
            _console = console;
            _tree = tree;
            _database = database;
            _id = id;
            _allDeletedData = null;
        }

        public bool Execute()
        {
            var deletedNodesId = _tree.RemoveNode(_id);

            if (deletedNodesId == null || deletedNodesId.IsEmpty)
            {
                return false;
            }

            _allDeletedData = new List<IData>();

            _console.WriteDiagnosticLine("Executing Delete command...");

            foreach (var deletedNodeId in deletedNodesId)
            {
                var deletedData = _database.Delete(deletedNodeId);
                if (deletedData == null)
                {
                    // Theoretically wouldn't happen, but put it for keep track in MSAI in case 
                    _console.WriteErrorLine($"Failed to deleted data from database when execute delete command");
                    continue;
                }

                _allDeletedData.Add(deletedData);
            }

            return _allDeletedData.Any();
        }

        public void Undo()
        {
            if (_allDeletedData != null)
            {
                _console.WriteDiagnosticLine("Executing Undo Delete command...");

                if (!_tree.BatchAddNewNode(_allDeletedData.ToImmutableList()))
                {
                    // Theoretically wouldn't happen, but put it for keep track in MSAI in case 
                    _console.WriteErrorLine($"Failed to create data into database when undo delete command");
                }

                foreach (var data in _allDeletedData)
                {
                    _database.Create(data);
                }

                _allDeletedData = null;
            }
        }
    }
}
