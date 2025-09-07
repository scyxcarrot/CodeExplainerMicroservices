using Rhino;
using Rhino.Commands;

namespace IDS.Common.Commands
{
    [System.Runtime.InteropServices.Guid("05e43a0e-aa0f-43e1-8fe6-c91ec0ae0c26")]
    public class StartScriptMode : Command
    {
        private static StartScriptMode _instance;

        public StartScriptMode()
        {
            _instance = this;
        }

        ///<summary>The only instance of the StartScriptMode command.</summary>
        public static StartScriptMode Instance => _instance;

        public override string EnglishName => "StartScriptMode";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            IDSPIAmacePlugIn.ScriptMode = true;

            return Result.Success;
        }
    }
}