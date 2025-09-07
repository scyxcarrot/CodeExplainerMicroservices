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
    [System.Runtime.InteropServices.Guid("640d97bd-ad90-4c55-9a42-3d70e760098b")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.ImplantSupport, IBB.Screw)]
    public class CMFImplantScrewQC : CmfCommandBase
    {
        static CMFImplantScrewQC _instance;
        public CMFImplantScrewQC()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFScrewQC command.</summary>
        public static CMFImplantScrewQC Instance => _instance;

        public override string EnglishName => "CMFImplantScrewQC";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            CMFScrewNumberBubbleConduitProxy.GetInstance().IsVisible = false;

            var proxy = CMFImplantScrewQcBubbleConduitProxy.Instance;
            var result = proxy.ToggleOnOff(director);

            var screwQcCheckRunStyleKey = "ScrewQcCheckRunStyle";
            if (proxy.IsVisible && result == Result.Success)
            {
                var timeTracking = proxy.GetCheckAllTimeTracker();

                foreach (var time in timeTracking)
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
