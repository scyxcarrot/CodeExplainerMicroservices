using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFDesignImplantVisualization : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.PlanningImplant, doc, false);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            var director = GetDirector(doc);

            if (director.CurrentDesignPhase == DesignPhase.Planning)
            {
                SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.PlanningImplant, doc, true);
            }
            else if (director.CurrentDesignPhase == DesignPhase.Implant)
            {
                SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.PlanningImplant, doc, false);
            }
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }

    }
}
