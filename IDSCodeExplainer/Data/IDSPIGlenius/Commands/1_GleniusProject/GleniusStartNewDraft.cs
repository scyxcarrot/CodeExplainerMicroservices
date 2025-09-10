using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Glenius.Enumerators;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("1fa2147e-2d53-4acf-92b1-87ffc1015500")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Draft)]
    public class GleniusStartNewDraft : CommandBase<GleniusImplantDirector>
    {
        public GleniusStartNewDraft()
        {
            Instance = this;
        }

        ///<summary>The only instance of the GleniusStartNewDraft command.</summary>
        public static GleniusStartNewDraft Instance { get; private set; }

        public override string EnglishName => "GleniusStartNewDraft";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            var startNewDraftOperator = new StartNewDraftOperator();
            var success = startNewDraftOperator.Execute(doc, director);
            return success ? Result.Success : Result.Failure;
        }
    }
}
