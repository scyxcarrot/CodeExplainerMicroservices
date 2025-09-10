using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Model;
using IDS.Interface.Tools;
using System;

namespace IDS.Core.V2.TreeDb.DbCommand
{
    public sealed class CreateCommand : IDbCommand 
    {
        private readonly IConsole _console;
        private readonly Tree _tree;
        private readonly IDatabase _database;
        private readonly IData _data;

        public CreateCommand(IConsole console, Tree tree, IDatabase database, IData data)
        {
            _console = console;
            _tree = tree;
            _database = database;
            _data = data;
        }

        public bool Execute()
        {
            _console.WriteDiagnosticLine("Executing Create command...");

            if (!_tree.AddNewNode(_data))
            {
                return false;
            }

            try
            {
                if (_database.Create(_data))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _console.WriteWarningLine($"Exception thrown from database: {ex.Message}; Reverting the operation.");
            }

            try
            {
                _tree.RemoveNode(_data.Id);
            }
            catch (Exception ex)
            {
                _console.WriteWarningLine($"Exception thrown when reverting the operation from database: {ex.Message};");
            }
            return false;
        }

        public void Undo()
        {
            _console.WriteDiagnosticLine("Executing Undo Create command...");

            var removedNodesId = _tree.RemoveNode(_data.Id);
            if (removedNodesId == null || removedNodesId.Count != 1)
            {
                // Theoretically wouldn't happen, but put it for keep track in MSAI in case 
                _console.WriteErrorLine(
                    $"Removed nodes when Undo create command: {(removedNodesId == null ? "null" : $"{removedNodesId.Count} nodes")}; " +
                    "State might out of sync, need to further investigate");
            }

            if (_database.Delete(_data.Id) == null)
            {
                // Theoretically wouldn't happen, but put it for keep track in MSAI in case 
                _console.WriteErrorLine($"Failed to deleted data from database when undo create command");
            }
        }
    }
}
