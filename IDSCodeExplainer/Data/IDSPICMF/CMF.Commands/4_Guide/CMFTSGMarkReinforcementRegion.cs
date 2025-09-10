using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.PICMF.Drawing;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.CMF
{
    [System.Runtime.InteropServices.Guid("C6BF0FDB-04B5-454A-9BC8-74E555EA6C81")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGMarkReinforcementRegion : CmfCommandBase
    {
        public CMFTSGMarkReinforcementRegion()
        {
            TheCommand = this;
            VisualizationComponent = new CMFTSGMarkRegionVisualization();
        }

        public static CMFTSGMarkReinforcementRegion TheCommand { get; private set; }
        public override string EnglishName => "CMFTSGMarkReinforcementRegion";

        // _-CMFTSGMarkReinforcementRegion TeethType Maxilla
        // _-CMFTSGMarkReinforcementRegion TeethType Mandible
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

            var visualizationComponent = (CMFTSGMarkRegionVisualization)VisualizationComponent;
            visualizationComponent.SetVisualizationDuringDrawing(
                doc, 
                isMandible);

            TeethSupportedGuideUtilities.GetLimitingSurfaces(director,
                limitingSurfaceIbb,
                out var limitingSurfaceIds,
                out var limitingSurface);
            var markSuccess = TeethSupportedGuideDrawingUtilities.MarkTSGSurface(
                director,
                director.CasePrefManager.GuidePreferences,
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

            var reinforcementRegionIbb = isMandible ?
                IBB.ReinforcementRegionMandible : IBB.ReinforcementRegionMaxilla;
            var objectManager = new CMFObjectManager(director);
            foreach (var surface in drawRegionResult.Regions)
            {
                var reinforcementId = IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    reinforcementRegionIbb,
                    limitingSurfaceIds,
                    surface.Patch
                );

                var rhinoObject = director.Document.Objects.Find(reinforcementId);
                surface.Serialize(rhinoObject.Attributes.UserDictionary);
            }

            var finalSupportWrappedIbb = isMandible ? IBB.FinalSupportWrappedMandible : IBB.FinalSupportWrappedMaxilla;
            var finalSupportWrappedIds = objectManager.GetAllBuildingBlockIds(finalSupportWrappedIbb).ToList();
            director.IdsDocument.Delete(finalSupportWrappedIds);

            return Result.Success;
        }
    }
}