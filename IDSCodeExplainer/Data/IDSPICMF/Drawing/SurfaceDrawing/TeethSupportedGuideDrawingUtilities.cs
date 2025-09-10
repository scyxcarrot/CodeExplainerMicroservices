using System.Collections.Generic;

using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;

using Rhino.Geometry;

namespace IDS.PICMF.Drawing
{
    public static class TeethSupportedGuideDrawingUtilities
    {
        public static bool MarkTSGSurface(
            CMFImplantDirector director,
            List<GuidePreferenceDataModel> guidePreferenceDataModelsToInvalidate,
            Mesh limitingSurface,
            out DrawRegionResult drawRegionResult)
        {
            drawRegionResult = null;
            var drawSurfaceDataContext = new DrawSurfaceDataContext();

            var drawSurface = new DrawRegion(
                drawSurfaceDataContext,
                limitingSurface);
            var prompt = "Press P to switch mode to Patch or Skeleton drawing. " +
                         "In Skeleton mode, press L to start a new Skeleton drawing.";
            drawSurface.SetCommandPrompt(prompt);

            if (!drawSurface.Execute())
            {
                return false;
            }

            drawRegionResult = drawSurface.RegionResult;
            if (drawRegionResult == null)
            {
                return false;
            }

            var guideCaseComponent = new GuideCaseComponent();
            var objectManager = new CMFObjectManager(director);

            foreach (var guidePreferenceDataModel in guidePreferenceDataModelsToInvalidate)
            {
                // this is needed to delete if the teeth block was imported
                var teethBlockEIbb = guideCaseComponent.GetGuideBuildingBlock(
                    IBB.TeethBlock, guidePreferenceDataModel);
                var teethBlockIds = objectManager.GetAllBuildingBlockIds(teethBlockEIbb);
                foreach (var teethBlockId in teethBlockIds)
                {
                    objectManager.DeleteObject(teethBlockId);
                }

                guidePreferenceDataModel.Graph.NotifyBuildingBlockHasChanged(
                    new[] { IBB.TeethBlock });
            }
            return true;
        }
    }
}
