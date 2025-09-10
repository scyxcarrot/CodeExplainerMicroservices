using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.PICMF.Drawing;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.CMF
{
    [System.Runtime.InteropServices.Guid("C6BF0FDB-04B5-454A-9BC8-74E559EA6C81")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGMarkTeethBaseRegion : CmfCommandBase
    {
        public CMFTSGMarkTeethBaseRegion()
        {
            TheCommand = this;
            VisualizationComponent = new CMFTSGMarkRegionVisualization();
        }

        public static CMFTSGMarkTeethBaseRegion TheCommand { get; private set; }
        public override string EnglishName => "CMFTSGMarkTeethBaseRegion";

        // _-CMFTSGMarkTeethBaseRegion TeethType Maxilla GuideCaseNumber 1
        // _-CMFTSGMarkTeethBaseRegion TeethType Mandibe GuideCaseNumber 1
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

            var success = GuidePreferencesHelper.PromptForGuideCaseNumber(
                director,
                out var guidePreferenceDataModel);
            if (!success)
            {
                return Result.Failure;
            }

            var isTeethBaseRegionInLimitingSurface = 
                TeethSupportedGuideUtilities.CheckTeethBaseRegionInLimitingSurface(
                    director, 
                    guidePreferenceDataModel, 
                    limitingSurfaceIbb, 
                    isMandible);
            if (!isTeethBaseRegionInLimitingSurface)
            {
                return Result.Failure;
            }

            TeethSupportedGuideUtilities.GetLimitingSurfaces(director,
                limitingSurfaceIbb,
                out var limitingSurfaceIds,
                out var limitingSurface);

            var visualizationComponent = (CMFTSGMarkRegionVisualization)VisualizationComponent;
            visualizationComponent.SetVisualizationDuringDrawing(
                doc,
                isMandible);

            var guidePreferenceDataModelsToInvalidate = new List<GuidePreferenceDataModel>()
            {
                guidePreferenceDataModel
            };

            var markSuccess = TeethSupportedGuideDrawingUtilities.MarkTSGSurface(
                director,
                guidePreferenceDataModelsToInvalidate,
                limitingSurface,
                out var drawRegionResult);

            CMFTSGMarkRegionVisualization.ChangeLimitingSurfaceTransparency(
                doc,
                isMandible,
                0);
            
            if (!markSuccess)
            {
                return Result.Failure;
            }

            var teethBaseRegionEIbb =
                GetTeethBaseEIbb(guidePreferenceDataModel);
            var objectManager = new CMFObjectManager(director);
            foreach (var surface in drawRegionResult.Regions)
            {
                var teethBaseId = IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    teethBaseRegionEIbb,
                    limitingSurfaceIds,
                    surface.Patch
                );

                var rhinoObject = director.Document.Objects.Find(teethBaseId);
                surface.Serialize(rhinoObject.Attributes.UserDictionary);
            }

            var guideCaseComponent = new GuideCaseComponent();
            var teethBlockEIbb = guideCaseComponent.GetGuideBuildingBlock(
                IBB.TeethBlock, guidePreferenceDataModel);
            var teethBlockIds = objectManager.GetAllBuildingBlockIds(teethBlockEIbb).ToList();
            director.IdsDocument.Delete(teethBlockIds);

            return Result.Success;
        }

        private static ExtendedImplantBuildingBlock GetTeethBaseEIbb(
            GuidePreferenceDataModel guidePreferenceDataModel)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var teethBaseRegionEIbb =
                guideCaseComponent.GetGuideBuildingBlock(
                    IBB.TeethBaseRegion, guidePreferenceDataModel);
            return teethBaseRegionEIbb;
        }
    }
}