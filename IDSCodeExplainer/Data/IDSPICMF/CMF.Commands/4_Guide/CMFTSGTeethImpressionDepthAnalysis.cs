using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("E8D30A4E-FDBA-41EE-A71F-52D0C58AF185")]
    [IDSCMFCommandAttributes(DesignPhase.TeethBlock)]
    public class CMFTSGTeethImpressionDepthAnalysis : CmfCommandBase
    {
        public CMFTSGTeethImpressionDepthAnalysis()
        {
            TheCommand = this;
            VisualizationComponent = new TSGAnalysisVisualization();
        }

        public static CMFTSGTeethImpressionDepthAnalysis TheCommand { get; private set; }

        public override string EnglishName => CommandEnglishName.CMFTSGTeethImpressionDepthAnalysis;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (TSGGuideCommandHelper.DisableIfHasActiveAnalysis(director))
            {
                return Result.Cancel;
            }

            if (!TSGGuideCommandHelper.PromptForCastPart(director, mode, (DrawSurfaceVisualization)VisualizationComponent,
                out _, out var selectedCastType))
            {
                return Result.Cancel;
            }            

            var manager = new CastAnalysisManager(director);

            if (!manager.HasInputsForTeethImpressionDepthAnalysis(selectedCastType))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Please make sure that the required parts are available!");
                return Result.Failure;
            }

            manager.PerformTeethImpressionDepthAnalysis(selectedCastType, out var triangleCenterDistances);

            var resources = new CMFResources();
            var displayModeSettingsFile = resources.IdsCmfSettingsFile;
            RhinoApp.RunScript($"-_OptionsImport \"{displayModeSettingsFile}\" AdvDisplay=Yes Display=Yes _Enter", false);

            manager.ApplyTeethImpressionDepthAnalysis(selectedCastType, triangleCenterDistances, true);

            var hasAccurateMaxDepth = manager.GetAccurateTeethImpressionMaxDepth(selectedCastType, out var actualMaxDepth);
            if (hasAccurateMaxDepth)
            {
                if (actualMaxDepth > CastAnalysisManager.MaxTeethImpressionDepthValue)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Accurate Maximum is more than {CastAnalysisManager.MaxTeethImpressionDepthValue}!");
                    IDSDialogHelper.ShowMessage($"Accurate Maximum is more than {CastAnalysisManager.MaxTeethImpressionDepthValue}!", "Warning",
                        ShowMessageButton.OK, ShowMessageIcon.Warning, mode, ShowMessageResult.OK);
                }
            }
            else
            {
                var estimatedMaxDepth = triangleCenterDistances.Max();
                if (estimatedMaxDepth > CastAnalysisManager.MaxTeethImpressionDepthValue)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Unable to compute accurate maximum value. Estimated Maximum is more than {CastAnalysisManager.MaxTeethImpressionDepthValue}!");
                    IDSDialogHelper.ShowMessage($"Unable to compute accurate maximum value.\nEstimated Maximum is more than {CastAnalysisManager.MaxTeethImpressionDepthValue}!", "Warning",
                        ShowMessageButton.OK, ShowMessageIcon.Warning, mode, ShowMessageResult.OK);
                }
            }

            AnalysisScaleConduit.ConduitProxy.LowerBound = CastAnalysisManager.MinTeethImpressionDepthValue;
            AnalysisScaleConduit.ConduitProxy.UpperBound = CastAnalysisManager.MaxTeethImpressionDepthValue;
            AnalysisScaleConduit.ConduitProxy.Title = "Teeth Impression Depth";
            AnalysisScaleConduit.ConduitProxy.Enabled = true;

            ((TSGAnalysisVisualization)VisualizationComponent).CastPartType = selectedCastType;
            ((TSGAnalysisVisualization)VisualizationComponent).ShowCastPart = true;
            ((TSGAnalysisVisualization)VisualizationComponent).ShowLimitingSurface = true;

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
