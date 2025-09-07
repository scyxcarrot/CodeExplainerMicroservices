using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("DC676451-F9FE-499E-802E-44D9BD46EBF2")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.Screw)]
    public class CMFToggleScrewInfoBubble : CmfCommandBase
    {
        static CMFToggleScrewInfoBubble _instance;
        public CMFToggleScrewInfoBubble()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFToggleScrewInfoBubble command.</summary>
        public static CMFToggleScrewInfoBubble Instance => _instance;

        public override string EnglishName => CommandEnglishName.CMFToggleScrewInfoBubble;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var scrManager = new ScrewManager(director);
            if (!scrManager.IsAllImplantScrewsCalibrated())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning,
                    "Implant screws not calibrated yet, " +
                    "screw information might be incorrect!");
            }

            var conduitProxyInstance = ScrewInfoConduitProxy.GetInstance();

            var isShowing = conduitProxyInstance.IsShowing();

            if (!isShowing)
            {
                conduitProxyInstance.SetUp(scrManager.GetAllScrews(false), director, true);
                conduitProxyInstance.Show(true);
            }
            else
            {
                conduitProxyInstance.Show(false);
                conduitProxyInstance.Reset();
            }

            return Result.Success;
        }
    }
}