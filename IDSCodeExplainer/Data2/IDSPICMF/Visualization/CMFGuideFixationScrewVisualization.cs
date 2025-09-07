using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public abstract class CMFGuideFixationScrewVisualization : CMFVisualizationComponentBase
    {
        public void GenericVisibility(RhinoDoc doc)
        {
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuideFixationScrew, doc, true);
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuideSurface, doc, true);

            //all surgery stages
            SetPartTypeVisibility(ProPlanImportPartType.Other, ProPlanImport.PreopLayer, doc, false);
            SetPartTypeVisibility(ProPlanImportPartType.Other, ProPlanImport.OriginalLayer, doc, false);
            SetPartTypeVisibility(ProPlanImportPartType.Other, ProPlanImport.PlannedLayer, doc, false);

            SetLayerVisibility(BuildingBlocks.Blocks[IBB.PlannedTeethWrapped].Layer, doc, false);
            SetLayerVisibility(BuildingBlocks.Blocks[IBB.NervesWrapped].Layer, doc, false);
        }

        public void ShowBarrel(RhinoDoc doc)
        {
            SetAllImplantExtendedBuildingBlockLayerVisibility(IBB.RegisteredBarrel, doc, true);
        }
    }
}
