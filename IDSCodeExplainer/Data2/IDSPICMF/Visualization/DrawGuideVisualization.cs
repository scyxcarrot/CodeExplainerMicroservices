using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino;

namespace IDS.PICMF.Visualization
{
    public class DrawGuideVisualization : CMFVisualizationComponentBase
    {
        public void SetToCommonVisualization(RhinoDoc doc, bool showPatches, bool showGuideSurface, bool showSurfaceWrap, bool showSupport, bool showOsteotomy)
        {
            HandlePreOpLayerVisibility(doc, false);
            HandlePlannedLayerVisibility(doc, false);
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.PositiveGuideDrawings, doc, showPatches);
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.NegativeGuideDrawing, doc, showPatches);
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuideSurface, doc, showGuideSurface);
        }

        public void SetToCommonVisualization(RhinoDoc doc, bool showPatches, bool showSolidSurfaces, bool showGuideSurface, bool showSurfaceWrap, bool showSupport, bool showOsteotomy)
        {
            SetToCommonVisualization(doc, showPatches, showGuideSurface, showSurfaceWrap, showSupport, showOsteotomy);
            SetAllGuideExtendedBuildingBlockLayerVisibility(IBB.GuideSolidSurface, doc, showSolidSurfaces);
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);
            ApplyTransparency(IBB.GuideSupport, director, Transparency.Medium, true);
        }

        public void SetGuideDrawingSurfacesVisibility(GuidePreferenceDataModel guidePref, RhinoDoc doc, bool isVisible)
        {
            var guideComponent = new GuideCaseComponent();
            var positiveDrawing = guideComponent.GetGuideBuildingBlock(IBB.PositiveGuideDrawings, guidePref);
            var negativeDrawing = guideComponent.GetGuideBuildingBlock(IBB.NegativeGuideDrawing, guidePref);
            SetBuildingBlockLayerVisibility(positiveDrawing, doc, isVisible);
            SetBuildingBlockLayerVisibility(negativeDrawing, doc, isVisible);
        }

        public void SetLinkDrawingSurfacesVisibility(GuidePreferenceDataModel guidePref, RhinoDoc doc, bool isVisible)
        {
            var guideComponent = new GuideCaseComponent();
            var linkSurface = guideComponent.GetGuideBuildingBlock(IBB.GuideLinkSurface, guidePref);
            SetBuildingBlockLayerVisibility(linkSurface, doc, isVisible);
        }

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SetToCommonVisualization(doc, true, false, true, true, true);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            SetToCommonVisualization(doc, false, true, false, true, false);
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);
            ApplyTransparency(IBB.GuideSupport, director, Transparency.Medium, true);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            SetToCommonVisualization(doc, false, true, false, true, false);
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);
            ApplyTransparency(IBB.GuideSupport, director, Transparency.Medium, true);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            SetToCommonVisualization(doc, false, true, false, true, false);
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);
            ApplyTransparency(IBB.GuideSupport, director, Transparency.Medium, true);
        }
    }
}
