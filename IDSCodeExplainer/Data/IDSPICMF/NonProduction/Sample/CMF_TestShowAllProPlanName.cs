using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("E9367B33-19FE-4F6B-9937-578A0793D037")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any )]
    public class CMF_TestShowAllProPlanName : CmfCommandBase
    {
        public CMF_TestShowAllProPlanName()
        {
            TheCommand = this;
        }

        public static CMF_TestShowAllProPlanName TheCommand { get; private set; }

        public override string EnglishName => "CMF_TestShowAllProPlanName";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var proPlanBuildingBlocks = objectManager.GetAllBuildingBlocks(IBB.ProPlanImport).ToList();
            IDSPluginHelper.WriteLine(LogCategory.Default, "All ProPlan Building Blocks Name: ");
            foreach (var proPlanBuildingBlock in proPlanBuildingBlocks)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"\t{proPlanBuildingBlock.Name}");
            }
            return Result.Success;
        }
    }
#endif
}
