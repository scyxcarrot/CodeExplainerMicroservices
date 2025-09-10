using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino;
using System.Collections.Generic;

namespace IDS.PICMF.Visualization
{
    public class CMFGuidePrefPanelVisualizationHelper : CMFVisualizationComponentBase
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

        public void GuidePrefPanelOpVisualization(GuidePreferenceDataModel guidePref, 
                                                  RhinoDoc doc, 
                                                  bool isVisible, 
                                                  bool restoreVisualization,
                                                  bool applyVisibilityToAllGuideComponents = false)
        {
            var guideComponent = new GuideCaseComponent();
            var blocks = new List<ExtendedImplantBuildingBlock>();

            if (applyVisibilityToAllGuideComponents)
            {
                blocks = guideComponent.GetGuideBuildingBlockList(guidePref);
            }
            else
            {
                var previewSmoothenBlock = guideComponent.GetGuideBuildingBlock(IBB.GuidePreviewSmoothen, guidePref);
                var actualGuide = guideComponent.GetGuideBuildingBlock(IBB.ActualGuide, guidePref);
                var smoothGuideBaseSurface = guideComponent.GetGuideBuildingBlock(IBB.SmoothGuideBaseSurface, guidePref);

                blocks.Add(previewSmoothenBlock);
                blocks.Add(actualGuide);
                blocks.Add(smoothGuideBaseSurface);
            }

            if (restoreVisualization)
            {
                StoreLayerVisibility(blocks, doc);
            }

            foreach (var extendedImplantBuildingBlock in blocks)
            {
                SetBuildingBlockLayerVisibility(extendedImplantBuildingBlock, doc, isVisible);
            }
        }

        public void ShowBarrels(CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel, bool isVisible=true)
        {
            var screwManager = new ScrewManager(director);
            var implantComponent = new ImplantCaseComponent();
            var linkedImplantScrewGuids = guidePrefModel.LinkedImplantScrews;

            foreach (var linkedImplantScrewGuid in linkedImplantScrewGuids)
            {
                var implantScrew = director.Document.Objects.Find(linkedImplantScrewGuid) as Screw;
                var implantCasePref = screwManager.GetImplantPreferenceTheScrewBelongsTo(implantScrew);
                var registeredBarrelIbb = implantComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, implantCasePref);
                SetBuildingBlockLayerVisibility(registeredBarrelIbb, director.Document, isVisible);
            }
        }
    }
}
