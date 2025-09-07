using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Drawing;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("342478A6-B474-4F08-B030-1960120625CF")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGEditBracketRegion : CmfCommandBase
    {
        public CMFTSGEditBracketRegion()
        {
            TheCommand = this;
            VisualizationComponent = new CMFTSGMarkRegionVisualization();
        }

        public static CMFTSGEditBracketRegion TheCommand { get; private set; }

        public override string EnglishName => "CMFTSGEditBracketRegion";

        // _-CMFTSGEditBracketRegion TeethType Mandible
        // _-CMFTSGEditBracketRegion TeethType Maxilla
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var isMandible = TeethSupportedGuideUtilities.AskUserTeethType();
            var limitingSurfaceIbb = isMandible ?
                IBB.LimitingSurfaceMandible : IBB.LimitingSurfaceMaxilla;

            var isIbbPresent = TeethSupportedGuideUtilities.CheckIfIbbsArePresent(
                director,
                new List<IBB>() { limitingSurfaceIbb }
            );
            if (!isIbbPresent)
            {
                return Result.Failure;
            }

            TeethSupportedGuideUtilities.GetLimitingSurfaces(director,
                limitingSurfaceIbb,
                out var limitingSurfaceIds,
                out var limitingSurface);

            var editSurface = new EditSurfaceHelper(
                limitingSurface,
                director);

            UnlockBracketRegion(director, isMandible);

            var bracketIbb = isMandible ?
                IBB.BracketRegionMandible : IBB.BracketRegionMaxilla;
            var patchDatas =
                TeethSupportedGuideUtilities.GetPatchDatas(
                    director,
                    bracketIbb);

            var visualizationComponent = (CMFTSGMarkRegionVisualization)VisualizationComponent;
            visualizationComponent.SetVisualizationDuringDrawing(
                doc,
                isMandible);
            var success = editSurface.Execute(patchDatas);
            CMFTSGMarkRegionVisualization.ChangeLimitingSurfaceTransparency(
                doc,
                isMandible,
                0);
            if (!success)
            {
                return Result.Failure;
            }

            var result = editSurface.EditSurfaceResult;
            if (result == null)
            {
                IDSPluginHelper.WriteLine(
                    LogCategory.Error,
                    "There is no result");
                return Result.Failure;
            }

            var objectManager = new CMFObjectManager(director);
            foreach (var surface in result.Surfaces)
            {
                director.IdsDocument.Delete(surface.Key);
                var baseId = IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    bracketIbb,
                    limitingSurfaceIds,
                    surface.Value.Patch
                );

                var rhinoObject = director.Document.Objects.Find(baseId);
                surface.Value.Serialize(rhinoObject.Attributes.UserDictionary);
            }

            var guideCaseComponent = new GuideCaseComponent();
            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
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

            return Result.Success;
        }

        private static void UnlockBracketRegion(
            CMFImplantDirector director,
            bool isMandible)
        {
            var objectManager = new CMFObjectManager(director);
            var bracketIbb = isMandible ? IBB.BracketRegionMandible : IBB.BracketRegionMaxilla;
            var bracketIds = 
                objectManager.GetAllBuildingBlockIds(bracketIbb);
            foreach (var bracketId in bracketIds)
            {
                director.Document.Objects.Unlock(bracketId, true);
            }
        }
    }
}
