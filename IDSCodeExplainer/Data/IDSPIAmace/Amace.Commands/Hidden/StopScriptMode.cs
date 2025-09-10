#if DEBUG

using Rhino;
using Rhino.Commands;

namespace IDS.Common.Commands
{
    [System.Runtime.InteropServices.Guid("6f61b80b-f8b0-4bdc-a492-ba8fd497c42d")]
    public class StopScriptMode : Command
    {
        private static StopScriptMode _instance;

        public StopScriptMode()
        {
            _instance = this;
        }

        ///<summary>The only instance of the StopScriptMode command.</summary>
        public static StopScriptMode Instance => _instance;

        public override string EnglishName => "StopScriptMode";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            IDSPIAmacePlugIn.ScriptMode = false;

            return Result.Success;
        }
    }
}

#endif