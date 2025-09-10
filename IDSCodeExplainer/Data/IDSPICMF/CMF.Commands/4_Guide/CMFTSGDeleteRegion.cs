using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("04FFD7DD-670D-4423-A627-32ABD75383FE")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGDeleteRegion : CmfCommandBase
    {
        public CMFTSGDeleteRegion()
        {
            TheCommand = this;
            VisualizationComponent = new CMFTSGMarkRegionVisualization();
        }

        public static CMFTSGDeleteRegion TheCommand { get; private set; }

        public override string EnglishName => "CMFTSGDeleteRegion";

        // _-CMFTSGDeleteRegion TeethType Mandible GuideCaseNumber 1
        // _-CMFTSGDeleteRegion TeethType Maxilla GuideCaseNumber 1
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var isMandible = TeethSupportedGuideUtilities.AskUserTeethType();
            var bracketRegionIbb =
                isMandible ? IBB.BracketRegionMandible : IBB.BracketRegionMaxilla;
            var reinforcementRegionIbb =
                isMandible ? IBB.ReinforcementRegionMandible : IBB.ReinforcementRegionMaxilla;

            var isIbbPresent = TeethSupportedGuideUtilities.CheckIfAnyIbbsArePresent(
                director,
                new List<IBB>()
                {
                    IBB.TeethBaseRegion, 
                    bracketRegionIbb, 
                    reinforcementRegionIbb
                }
            );
            if (!isIbbPresent)
            {
                return Result.Failure;
            }

            var visualizationComponent = (CMFTSGMarkRegionVisualization)VisualizationComponent;
            visualizationComponent.SetVisualizationDuringDrawing(
                doc,
                isMandible);
            var success = TeethSupportedGuideUtilities.DeleteSurfaces(
                director,
                isMandible);
            if (success)
            {
                var guideCaseComponent = new GuideCaseComponent();
                var objectManager = new CMFObjectManager(director);
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
            }

            CMFTSGMarkRegionVisualization.ChangeLimitingSurfaceTransparency(doc, isMandible, 0);

            return success ? Result.Success : Result.Cancel;
        }
    }
}