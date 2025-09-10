using IDS.Core.V2.TreeDb.Interface;
using System.Collections.Generic;

namespace IDS.Core.V2.TreeDb.DbCommand
{
    public sealed class MultiCommand : IDbCommand 
    {
        private readonly IList<IDbCommand> _commandList;

        public MultiCommand(IList<IDbCommand> commandList)
        {
            _commandList = commandList;
        }

        public bool Execute()
        {
            foreach (var command in _commandList)
            {
                if (!command.Execute())
                {
                    return false;
                }
            }

            return true;
        }

        public void Undo()
        {
            for (var i = _commandList.Count - 1; i >= 0; i--)
            {
                var command = _commandList[i];
                command.Undo();
            }
        }
    }
}
