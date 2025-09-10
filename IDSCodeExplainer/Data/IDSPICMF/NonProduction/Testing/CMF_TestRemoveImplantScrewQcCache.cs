using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("CE88577F-F8B6-41EB-9373-3DAB2B8268FC")]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMF_TestRemoveImplantScrewQcCache : CmfCommandBase
    {
        public CMF_TestRemoveImplantScrewQcCache()
        {
            Instance = this;
        }

        public static CMF_TestRemoveImplantScrewQcCache Instance { get; private set; }

        public override string EnglishName => "CMF_TestRemoveImplantScrewQcCache";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            director.ImplantScrewQcLiveUpdateHandler = null;
            return Result.Success;
        }
    }

#endif
}
