using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Model;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IDS.Core.V2.TreeDb.DbCommand
{
    public class BatchCreateCommand : IDbCommand
    {
        private readonly IConsole _console;
        private readonly Tree _tree;
        private readonly IDatabase _database;
        private readonly ImmutableList<IData> _batchData;

        public BatchCreateCommand(
            IConsole console,
            Tree tree,
            IDatabase database,
            IList<IData> batchData)
        {
            _console = console;
            _tree = tree;
            _database = database;
            _batchData = batchData.ToImmutableList();
        }

        public bool Execute()
        {
            _console.WriteDiagnosticLine("Executing BatchCreate command...");

            if (!_tree.BatchAddNewNode(_batchData))
            {
                return false;
            }

            try
            {
                var allSuccess = _batchData.All(data => _database.Create(data));

                if (allSuccess)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _console.WriteWarningLine($"Exception thrown from database: {ex.Message}; Reverting the operation.");
            }

            foreach (var data in _batchData)
            {
                _tree.RemoveNode(data.Id);
                try
                {
                    _database.Delete(data.Id);
                }
                catch (Exception ex)
                {
                    _console.WriteWarningLine($"Exception thrown when reverting the operation from database: {ex.Message};");
                }
            }

            return false;

        }

        public void Undo()
        {
            _console.WriteDiagnosticLine("Executing Undo BatchCreate command...");

            foreach (var data in _batchData)
            {
                var removedNodesId = _tree.RemoveNode(data.Id);
                if (removedNodesId == null || removedNodesId.Count != 1)
                {
                    // Theoretically wouldn't happen, but put it for keep track in MSAI in case 
                    _console.WriteErrorLine(
                        $"Removed nodes when Undo create command: {(removedNodesId == null ? "null" : $"{removedNodesId.Count} nodes")}; " +
                        "State might out of sync, need to further investigate");
                }

                if (_database.Delete(data.Id) == null)
                {
                    // Theoretically wouldn't happen, but put it for keep track in MSAI in case 
                    _console.WriteErrorLine($"Failed to deleted data from database when undo create command");
                }
            }
        }
    }
}
