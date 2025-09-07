using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("0C8BD2F7-ACCF-4C46-AB12-E9F94DA8B2DD")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    public class CMFToggleGuideFixationScrewInfoBubble : CmfCommandBase
    {
        static CMFToggleGuideFixationScrewInfoBubble _instance;
        public CMFToggleGuideFixationScrewInfoBubble()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFToggleGuideFixationScrewInfoBubble command.</summary>
        public static CMFToggleGuideFixationScrewInfoBubble Instance => _instance;

        public override string EnglishName => CommandEnglishName.CMFToggleGuideFixationScrewInfoBubble;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            CMFGuideScrewQcBubbleConduitProxy.Instance.TurnOff();

            var screwManager = new ScrewManager(director);

            var conduitProxyInstance = ScrewInfoConduitProxy.GetInstance();

            var isShowing = conduitProxyInstance.IsShowing();

            if (!isShowing)
            {
                conduitProxyInstance.SetUp(screwManager.GetAllScrews(true), director, false);
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