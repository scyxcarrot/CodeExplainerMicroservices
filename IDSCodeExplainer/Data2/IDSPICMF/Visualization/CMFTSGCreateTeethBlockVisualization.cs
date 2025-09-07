using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFTSGCreateTeethBlockVisualization : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
        }

        public void ShowTeethBlockOnly(
            RhinoDoc doc, 
            GuidePreferenceDataModel guidePreferenceDataModel)
        {
            HideAllLayerVisibility(doc);
            var guideCaseComponent = new GuideCaseComponent();
            var teethBlockEIbb = guideCaseComponent.GetGuideBuildingBlock(
                IBB.TeethBlock, guidePreferenceDataModel);

            SetBuildingBlockLayerVisibility(teethBlockEIbb, doc, true);
        }
    }
}
