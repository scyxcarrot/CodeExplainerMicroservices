using IDS.CMF;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.CommandBase;
using IDS.Core.PluginHelper;
using Rhino;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Visualization
{
    public class CMFStartGuidePhaseVisualization : ICommandVisualizationComponent
    {
        private void SuccessVisibility(RhinoDoc doc)
        {
            var showPaths = new List<string>();

            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
            var objManager = new CMFObjectManager(director);

            var screwRegistration = new ScrewRegistration(director, true);
            var screwManager = new ScrewManager(director);
            var allScrews = screwManager.GetAllScrews(false);
            foreach (var screw in allScrews)
            {
                var result =
                    screwRegistration
                        .PerformImplantScrewRegistrationToOriginalPosition(screw);
                var originalMesh = result.RegisteredOnOriginalMeshObject;

                if (originalMesh != null)
                {
                    showPaths.Add(originalMesh.LayerPath);
                }
            }

            showPaths.Add(BuildingBlocks.Blocks[IBB.GuideFlangeGuidingOutline].Layer);

            var guidePreferences = director.CasePrefManager.GuidePreferences;

            var guideIbbListToShow = new List<IBB>
            {
                IBB.GuidePreviewSmoothen,
                IBB.GuideFixationScrew,
                IBB.TeethBlock,
                IBB.GuideFixationScrewEye,
                IBB.GuideFixationScrewLabelTag,
                IBB.GuideSurface,
                IBB.GuideBridge,
                IBB.GuideFlange,
            };
            var guideCaseComponent = new GuideCaseComponent();
            var extendedIbbList = 
                guidePreferences.SelectMany(guidePreference => guideIbbListToShow.Select(
                    guideIbb => guideCaseComponent.GetGuideBuildingBlock(
                        guideIbb, guidePreference)));
            var layerPathsToShow = 
                extendedIbbList.Select(extendedIbb => extendedIbb.Block.Layer).ToList();
            showPaths.AddRange(layerPathsToShow);

            var registeredBarrelShowPaths =
                objManager.GetAllImplantBuildingBlocks(IBB.RegisteredBarrel)
                    .Select(buildingBlock => buildingBlock.Layer).ToList();
            showPaths.AddRange(registeredBarrelShowPaths);

            Core.Visualization.Visibility.SetVisible(doc, showPaths);
        }

        public void OnCommandBeginVisualization(RhinoDoc doc)
        {

        }

        public void OnCommandCanceledVisualization(RhinoDoc doc)
        {

        }

        public void OnCommandFailureVisualization(RhinoDoc doc)
        {

        }

        public void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            SuccessVisibility(doc);
        }
    }
}
