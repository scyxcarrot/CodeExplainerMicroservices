using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.ScrewQc;
using IDS.CMF.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("0a0dc910-1ee5-4ee6-b6f5-aafcb2250b50")]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    [CommandStyle(Style.ScriptRunner)]
    public class CMFShowAllGuideFixationScrewGauges : CmfCommandBase
    {
        static CMFShowAllGuideFixationScrewGauges _instance;
        public CMFShowAllGuideFixationScrewGauges()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFShowAllGuideFixationScrewGauges command.</summary>
        public static CMFShowAllGuideFixationScrewGauges Instance => _instance;

        public override string EnglishName => "CMFShowAllGuideFixationScrewGauges";

        public override bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!base.CheckCommandCanExecute(doc, mode, director))
            {
                return false;
            }

            return !(CMFGuideScrewQcBubbleConduitProxy.Instance.IsVisible);
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var proxy = AllGuideFixationScrewGaugesProxy.Instance;
            proxy.InitializeConduit(director);
            proxy.IsEnabled = !proxy.IsEnabled;

            return Result.Success;
        }
    }
}
