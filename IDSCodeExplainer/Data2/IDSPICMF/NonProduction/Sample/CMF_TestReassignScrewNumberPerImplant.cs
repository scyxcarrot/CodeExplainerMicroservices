using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;
using Rhino.Commands;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("65BFC498-DA8F-48FA-B1D2-29D59D6EDDAC")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestReassignScrewNumberPerImplant : CmfCommandBase
    {
        public CMF_TestReassignScrewNumberPerImplant()
        {
            Instance = this;
        }
        
        public static CMF_TestReassignScrewNumberPerImplant Instance { get; private set; }

        public override string EnglishName => "CMF_TestReassignScrewNumberPerImplant";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var implantComponent = new ImplantCaseComponent();

            foreach (var casePreference in director.CasePrefManager.CasePreferences)
            {
                var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreference);
                var screws = objectManager.GetAllBuildingBlocks(screwBuildingBlock).ToList();
                RhinoApp.WriteLine($"{casePreference.CaseName}: {screws.Count} screws");
                for (var i = 0; i < screws.Count; i++)
                {
                    var screw = (Screw)screws[i];
                    screw.Index = i + 1;
                }
            }

            return Result.Success;
        }
    }

#endif
}
