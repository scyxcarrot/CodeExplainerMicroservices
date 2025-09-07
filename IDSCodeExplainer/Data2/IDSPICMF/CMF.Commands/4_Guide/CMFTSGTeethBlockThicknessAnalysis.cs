using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("8F29B287-751C-4B83-9EE7-3552142FDD9E")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGTeethBlockThicknessAnalysis : CmfCommandBase
    {
        public CMFTSGTeethBlockThicknessAnalysis()
        {
            TheCommand = this;
            VisualizationComponent = new TSGAnalysisVisualization();
        }

        public static CMFTSGTeethBlockThicknessAnalysis TheCommand { get; private set; }

        public override string EnglishName => CommandEnglishName.CMFTSGTeethBlockThicknessAnalysis;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (TSGGuideCommandHelper.DisableIfHasActiveAnalysis(director))
            {
                return Result.Cancel;
            }

            var success = GuidePreferencesHelper.PromptForGuideCaseNumber(
                director,
                out var guidePrefData);
            if (!success)
            {
                return Result.Failure;
            }

            var objectManager = new CMFObjectManager(director);

            var guideComponent = new GuideCaseComponent();
            var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.TeethBlock, guidePrefData);
            if (!objectManager.HasBuildingBlock(extendedBuildingBlock))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"{guidePrefData.CaseName} does not have TeethBlock!");
                return Result.Failure;
            }

            var manager = new TeethBlockAnalysisManager(director);

            manager.PerformThicknessAnalysis(guidePrefData, out var thicknessData);

            var resources = new CMFResources();
            var displayModeSettingsFile = resources.IdsCmfSettingsFile;
            RhinoApp.RunScript($"-_OptionsImport \"{displayModeSettingsFile}\" AdvDisplay=Yes Display=Yes _Enter", false);

            manager.ApplyThicknessAnalysis(guidePrefData, thicknessData, true, out var lowerBound, out var upperBound);

            AnalysisScaleConduit.ConduitProxy.LowerBound = lowerBound;
            AnalysisScaleConduit.ConduitProxy.UpperBound = upperBound;
            AnalysisScaleConduit.ConduitProxy.Title = "Teeth Block Thickness";
            AnalysisScaleConduit.ConduitProxy.Enabled = true;

            ((TSGAnalysisVisualization)VisualizationComponent).GuidePreferenceDataModel = guidePrefData;
            ((TSGAnalysisVisualization)VisualizationComponent).ShowTeethBlock = true;

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
