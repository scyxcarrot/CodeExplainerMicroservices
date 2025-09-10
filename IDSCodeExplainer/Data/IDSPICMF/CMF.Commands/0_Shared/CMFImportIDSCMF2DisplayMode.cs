using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("4D68C438-5C10-4B10-A951-CF117B4D8BC6")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMFImportIDSCMF2DisplayMode : CmfCommandBase
    {
        public CMFImportIDSCMF2DisplayMode()
        {
            Instance = this;
        }

        public static CMFImportIDSCMF2DisplayMode Instance { get; private set; }

        public override string EnglishName => "CMFImportIDSCMF2DisplayMode";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            View.ImportIDSCMF2DisplayMode();

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
