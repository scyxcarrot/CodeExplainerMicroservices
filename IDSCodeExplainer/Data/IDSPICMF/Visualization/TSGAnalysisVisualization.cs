using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Visualization
{
    public class TSGAnalysisVisualization : DrawSurfaceVisualization
    {
        public ProPlanImportPartType CastPartType { get; set; } = ProPlanImportPartType.NonProPlanItem;
        public bool ShowCastPart { get; set; } = false;
        public bool ShowLimitingSurface { get; set; } = false;
        public GuidePreferenceDataModel GuidePreferenceDataModel { get; set; } = null;
        public bool ShowTeethBlock { get; set; } = false;

        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            //do nothing
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            //do nothing
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            //do nothing
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            HideAllLayerVisibility(doc);
            SetCastPartVisibility(doc);
            SetLimitingSurfaceVisibility(doc);
            SetTeethBlockVisibility(doc);
            doc.Views.ActiveView.ActiveViewport.ZoomExtents();
        }

        private void SetCastPartVisibility(RhinoDoc doc)
        {
            if (!ShowCastPart || (CastPartType != ProPlanImportPartType.MandibleCast && CastPartType != ProPlanImportPartType.MaxillaCast))
            {
                return;
            }

            HandleOriginalLayerVisibility(doc, true);

            var originalCastRhinoObject = ProPlanImportUtilities.GetAllProplanPartsAsRangePartType(
               doc, ProplanBoneType.Original, new List<ProPlanImportPartType>()
               {
                    CastPartType
               }).FirstOrDefault();

            var path = doc.Layers[originalCastRhinoObject.Attributes.LayerIndex].FullPath;
            SetLayerVisibility(path, doc, true);
        }

        private void SetLimitingSurfaceVisibility(RhinoDoc doc)
        {
            if (!ShowLimitingSurface || (CastPartType != ProPlanImportPartType.MandibleCast && CastPartType != ProPlanImportPartType.MaxillaCast))
            {
                return;
            }

            SetLayerVisibility(LayerName.TeethSupportedGuide, doc, true);

            var buildingBlock = CastPartType == ProPlanImportPartType.MandibleCast ? IBB.LimitingSurfaceMandible : IBB.LimitingSurfaceMaxilla;
            SetBuildingBlockLayerVisibility(buildingBlock, doc, true);
        }

        private void SetTeethBlockVisibility(RhinoDoc doc)
        {
            if (!ShowTeethBlock || GuidePreferenceDataModel == null)
            {
                return;
            }

            var guideComponent = new GuideCaseComponent();
            var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.TeethBlock, GuidePreferenceDataModel);
            SetBuildingBlockLayerVisibility(extendedBuildingBlock, doc, true);
        }
    }
}
