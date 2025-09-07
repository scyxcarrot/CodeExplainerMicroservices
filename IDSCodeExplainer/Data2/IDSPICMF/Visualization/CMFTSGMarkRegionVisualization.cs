using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Visualization
{
    public class CMFTSGMarkRegionVisualization : CMFVisualizationComponentBase
    {
        public void SetVisualizationDuringDrawing(
            RhinoDoc doc,
            bool isMandible)
        {
            HideAllLayerVisibility(doc);
            ToggleCastVisibility(doc, isMandible, true);

            var ibbsToShow = new List<IBB>()
            {
                IBB.LimitingSurfaceMaxilla,
                IBB.BracketRegionMaxilla,
                IBB.ReinforcementRegionMaxilla,
            };

            if (isMandible)
            {
                ibbsToShow = new List<IBB>()
                {
                    IBB.LimitingSurfaceMandible,
                    IBB.BracketRegionMandible,
                    IBB.ReinforcementRegionMandible,
                };
            }
            
            foreach (var ibb in ibbsToShow)
            {
                SetBuildingBlockLayerVisibility(ibb, doc, true);
            }
            ChangeLimitingSurfaceTransparency(doc, isMandible, 0.4);

            var guideCaseComponent = new GuideCaseComponent();
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                var teethBaseEIbb = 
                    guideCaseComponent.GetGuideBuildingBlock(
                        IBB.TeethBaseRegion, 
                        guidePreferenceDataModel);
                SetBuildingBlockLayerVisibility(teethBaseEIbb, doc, true);
            }
        }

        public static void ChangeLimitingSurfaceTransparency(
            RhinoDoc doc, 
            bool isMandible, 
            double transparency)
        {
            var limitingSurfaceIbb = isMandible ? IBB.LimitingSurfaceMandible : IBB.LimitingSurfaceMaxilla;

            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(
                (int)doc.RuntimeSerialNumber);
            var objectManager = new CMFObjectManager(director);
            foreach (var rhinoObject in objectManager.GetAllBuildingBlocks(limitingSurfaceIbb))
            {
                var material = rhinoObject.GetMaterial(true);
                material.Transparency = transparency;
                material.CommitChanges();
            }
        }

        private void ToggleCastVisibility(RhinoDoc doc, bool isMandible, bool isVisible)
        {
            var proPlanComponent = new ProPlanImportComponent();
            var proPlanPartType = isMandible ?
                ProPlanImportPartType.MandibleCast : ProPlanImportPartType.MaxillaCast;
            var proPlanImportBlocks = proPlanComponent.Blocks
                .Where(x => x.PartType == proPlanPartType);
            var eIbbs =
                proPlanImportBlocks.Select(x => proPlanComponent.GetProPlanImportBuildingBlock(x.PartNamePattern));
            foreach (var eIbb in eIbbs)
            {
                SetBuildingBlockLayerVisibility(eIbb, doc, isVisible);
            }
        }

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
    }
}
