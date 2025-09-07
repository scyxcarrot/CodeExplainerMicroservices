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
    [System.Runtime.InteropServices.Guid("7E7AFDF1-CE04-4C4E-9177-059FD53F8C64")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    public class CMFGuideFixationScrewQC : CmfCommandBase
    {
        private static CMFGuideFixationScrewQC _instance;

        public CMFGuideFixationScrewQC()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFScrewQC command.</summary>
        public static CMFGuideFixationScrewQC Instance => _instance;

        public override string EnglishName => "CMFGuideFixationScrewQC";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            CMFScrewNumberBubbleConduitProxy.GetInstance().IsVisible = false;

            var proxy = CMFGuideScrewQcBubbleConduitProxy.Instance;
            var result = proxy.ToggleOnOff(director);

            var screwQcCheckRunStyleKey = "ScrewQcCheckRunStyle";
            if (proxy.IsVisible && result == Result.Success)
            {
                var timeTracker = proxy.GetCheckAllTimeTracker();

                foreach (var time in timeTracker)
                {
                    AddTrackingParameterSafely(time.Key, $"{time.Value * 0.001}");
                }

                AddTrackingParameterSafely(screwQcCheckRunStyleKey, ScrewQcCheckRunStyle.KeyOnScrewQcCheck);
            }
            else if (!proxy.IsVisible && result == Result.Success)
            {
                AddTrackingParameterSafely(screwQcCheckRunStyleKey, ScrewQcCheckRunStyle.KeyOffScrewQcCheck);
            }
            else if (result == Result.Failure)
            {
                AddTrackingParameterSafely(screwQcCheckRunStyleKey, ScrewQcCheckRunStyle.KeyFailScrewQcCheck);
            }

            return result;
        }
    }
}
