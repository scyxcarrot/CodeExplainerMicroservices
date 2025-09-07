using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.PICMF.Commands;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class CMFImplantSupportVisualization : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            //
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            HandlePreOpLayerVisibility(doc, false);
            HandleOriginalLayerVisibility(doc, false);
            HandlePlannedLayerVisibility(doc, true);

            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.Screw, doc, true);
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.Connection, doc, true);

            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.ImplantSupport, doc, true);
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)doc.RuntimeSerialNumber);
            CMFToggleTransparency.Instance.ApplyImplantSupportCustomTransparencies(director);
        }
        
        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            //
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            //
        }
    }
}
