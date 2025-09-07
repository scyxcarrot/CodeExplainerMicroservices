using IDS.CMF;
using IDS.CMF.AttentionPointer;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("1D0F8170-BCED-46D7-BBAD-6763C1FFD48F")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant)]
    public class CMFToggleHighlightPastille : CmfCommandBase
    {
        public CMFToggleHighlightPastille()
        {
            Instance = this;
        }

        public static CMFToggleHighlightPastille Instance { get; private set; }

        public override string EnglishName => "CMFToggleHighlightPastille";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            PastilleAttentionPointer.Instance.ToggleHighlightPastille(director);
            return Result.Success;
        }
    }
}
