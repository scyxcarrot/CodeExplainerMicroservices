using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Helper;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("04F0AD35-0E02-4CCE-A68D-60D5EABE7385")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideSurfaceWrap, IBB.GuideFlangeGuidingOutline)]
    public class CMFCreateGuideFlange : CmfCommandBase
    {
        public CMFCreateGuideFlange()
        {
            TheCommand = this;
            VisualizationComponent = new CMFGuideFlangeVisualization();
        }

        public static CMFCreateGuideFlange TheCommand { get; private set; }

        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper { get; } =
            new CMFGuidePrefPanelVisualizationHelper();

        public override string EnglishName => "CMFCreateGuideFlange";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var guideCaseGuid = GuidePreferencesHelper.PromptForPreferenceId();

            if (guideCaseGuid == Guid.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Guide preference not found!");
                return Result.Failure;
            }

            var objectManager = new CMFObjectManager(director);

            var guidePrefModel = objectManager.GetGuidePreference(guideCaseGuid);

            GuidePrefPanelVisualizationHelper.GuidePrefPanelOpVisualization(guidePrefModel, doc, false, true);

            var flangeCurveInputGetter = new GuideFlangeInputsGetter(director);
            var res = flangeCurveInputGetter.GetInputs();
            if (res == GuideFlangeInputsGetter.EResult.Success)
            {
                var createGuideFlange = new GuideFlangeCreation(director, flangeCurveInputGetter.OsteotomyParts);
                Mesh outputFlange;
                if (!createGuideFlange.GenerateGuideFlange(flangeCurveInputGetter.FlangeCurve, flangeCurveInputGetter.FlangeHeight, out outputFlange))
                {
                    return Result.Failure;
                }

                var helper = new GuideFlangeObjectHelper(director);
                helper.AddNewFlange(guidePrefModel, outputFlange, flangeCurveInputGetter.FlangeCurve, flangeCurveInputGetter.FlangeHeight);

                guidePrefModel.Graph.InvalidateGraph();
                guidePrefModel.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideFlange });

                return Result.Success;
            }

            return Result.Failure;
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
        }
    }
}
