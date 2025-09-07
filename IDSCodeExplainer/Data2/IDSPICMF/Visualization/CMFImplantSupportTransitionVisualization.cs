using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public interface IImplantTransitionVisualization
    {
        void OnCutModeSelected();

        void OnMarginModeSelected();

        void OnBoneModeSelected();
    }

    public class CMFImplantSupportTransitionVisualization : CMFImplantTransitionVisualization, IImplantTransitionVisualization
    {
        public void OnCutModeSelected()
        {
            SetBuildingBlockLayerVisibility(IBB.ImplantSupportGuidingOutline, RhinoDoc.ActiveDoc, true);
        }

        public void OnMarginModeSelected()
        {
            SetBuildingBlockLayerVisibility(IBB.ImplantMargin, RhinoDoc.ActiveDoc, true);
        }

        public void OnBoneModeSelected()
        {
            HandlePlannedLayerVisibility(RhinoDoc.ActiveDoc, true);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.Connection, RhinoDoc.ActiveDoc, true);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.Screw, RhinoDoc.ActiveDoc, true);
            SetBuildingBlockLayerVisibility(IBB.ImplantMargin, RhinoDoc.ActiveDoc, true);
        }
    }
}
