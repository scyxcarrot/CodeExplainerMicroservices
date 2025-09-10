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
    [System.Runtime.InteropServices.Guid("C6BF0FDB-04B5-459A-9BC8-74E555EA6C85")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGMarkBracketRegion : CmfCommandBase
    {
        public CMFTSGMarkBracketRegion()
        {
            TheCommand = this;
            VisualizationComponent = new CMFTSGMarkRegionVisualization();
        }

        public static CMFTSGMarkBracketRegion TheCommand { get; private set; }
        public override string EnglishName => "CMFTSGMarkBracketRegion";

        // _-CMFTSGMarkBracketRegion TeethType Maxilla
        // _-CMFTSGMarkBracketRegion TeethType Mandible
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

            var bracketRegionIbb = isMandible ?
                IBB.BracketRegionMandible : IBB.BracketRegionMaxilla;
            var objectManager = new CMFObjectManager(director);
            foreach (var surface in drawRegionResult.Regions)
            {
                var bracketId = IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(
                    objectManager,
                    director.IdsDocument,
                    bracketRegionIbb,
                    limitingSurfaceIds,
                    surface.Patch
                );

                var rhinoObject = director.Document.Objects.Find(bracketId);
                surface.Serialize(rhinoObject.Attributes.UserDictionary);

                var teethBlockRoiIbb = isMandible ? IBB.TeethBlockROIMandible : IBB.TeethBlockROIMaxilla;
                var teethBlockRoiIds = objectManager.GetAllBuildingBlockIds(teethBlockRoiIbb).ToList();
                director.IdsDocument.Delete(teethBlockRoiIds);
            }
            
            return Result.Success;
        }
    }
}