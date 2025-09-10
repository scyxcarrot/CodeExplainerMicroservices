using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("85bd20c0-8e28-433e-b9ea-ad5bfdf8101c")]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.Screw)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_ShowAllImplantScrewGauges : CmfCommandBase
    {
        private static CMF_ShowAllImplantScrewGauges _instance;
        public override string EnglishName => "CMF_ShowAllImplantScrewGauges";

        public CMF_ShowAllImplantScrewGauges()
        {
            _instance = this;
        }

        public static CMF_ShowAllImplantScrewGauges Instance => _instance;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var screwManager = new ScrewManager(director);
            if (!screwManager.IsAllImplantScrewsCalibrated())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, 
                    "Implant screws not calibrated yet, " +
                    "screw gauge location might be incorrect!");
            }

            var proxy = AllScrewGaugesProxy.Instance;
            proxy.InitializeConduit(director);
            proxy.IsEnabled = !proxy.IsEnabled;

            return Result.Success;
        }
    }
}
