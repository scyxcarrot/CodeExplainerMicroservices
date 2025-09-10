using IDS.Core.V2.TreeDb.Interface;
using IDS.Interface.Tools;

namespace IDS.Core.V2.TreeDb.DbCommand
{
    public sealed class EmptyCommand : IDbCommand
    {
        private readonly IConsole _console;

        public EmptyCommand(IConsole console)
        {
            _console = console;
        }

        public bool Execute()
        {
            _console.WriteLine($"Empty command");
            return true;
        }

        public void Undo()
        {
            _console.WriteLine($"Nothing to undo from database");
        }
    }
}
