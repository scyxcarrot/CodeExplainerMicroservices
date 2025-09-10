using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Relations;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.UI;
using RhinoMtlsCore.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("16A7C0EE-C899-4A5A-8031-7191062D60AA")]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideSurfaceWrap)]
    public class CMFGuidePreview : CmfCommandBase
    {
        public CMFGuidePreview()
        {
            TheCommand = this;
        }

        public static CMFGuidePreview TheCommand { get; private set; }

        public override string EnglishName => "CMFGuidePreview";

        private List<KeyValuePair<string, string>> _successfulGuides = new List<KeyValuePair<string, string>>();
        private List<KeyValuePair<string, string>> _unsuccessfulGuides = new List<KeyValuePair<string, string>>();
        private List<KeyValuePair<string, string>> _skippedGuides = new List<KeyValuePair<string, string>>();

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            _successfulGuides = new List<KeyValuePair<string, string>>();
            _unsuccessfulGuides = new List<KeyValuePair<string, string>>();
            _skippedGuides = new List<KeyValuePair<string, string>>();

            if (!PhaseChanger.IsAllImplantsAndGuidesAreNumbered(director))
            {
                return Result.Failure;
            }

            // as of REQ1118759, we will always generate smooth guide, no more coarse guide
            GenerateGuidePreviews(doc, director);

            if (_successfulGuides.Any())
            {
                TrackingParameters.Add($"Success Guides", string.Join(",", _successfulGuides.Select(x => x.Value)));
            }
            if (_unsuccessfulGuides.Any())
            {
                TrackingParameters.Add($"Failed Guides", string.Join(",", _unsuccessfulGuides.Select(x => x.Value)));
            }
            if (_skippedGuides.Any())
            {
                TrackingParameters.Add($"Skipped Guides", string.Join(",", _skippedGuides.Select(x => x.Value)));
            }

            return Result.Success;
        }

        private void GenerateGuidePreviews(RhinoDoc doc, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);

            var guidesAndProcessingTimes = new Dictionary<string, TimeSpan>();
            var guideProcessingInputMeshesInfo = new List<GuideCreatorV2.InputMeshesInfo>();
            var failedGuidesInfo = new List<string>();

            // generate the coarse /  smooth guide preview directly instead of two steps as of REQ1118759
            GenerateGuidePreviews(doc, director, objectManager, ref guidesAndProcessingTimes, ref guideProcessingInputMeshesInfo, ref failedGuidesInfo);

            doc.Views.Redraw();

            var guideSupport = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            var guideSurfaceWrap = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap).Geometry;

            IDSPluginHelper.WriteLine(LogCategory.Default, "Guide Preview Summary");
            IDSPluginHelper.WriteLine(LogCategory.Default, " > Processing Time");
            foreach (var guideAndProcessingTime in guidesAndProcessingTimes.OrderBy(p => p.Key))
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"    > {guideAndProcessingTime.Key} Preview took {guideAndProcessingTime.Value:mm\\:ss} to create.");
            }
            IDSPluginHelper.WriteLine(LogCategory.Default, " > Mesh Sizes");
            IDSPluginHelper.WriteLine(LogCategory.Default, $"    > Entire Guide Support has {guideSupport.Faces.Count} triangles");
            IDSPluginHelper.WriteLine(LogCategory.Default, $"    > Entire Guide Surface Wrap has {guideSurfaceWrap.Faces.Count} triangles");

            TrackingParameters.Add($"Guide Support Mesh N Triangles", guideSupport.Faces.Count.ToString());
            TrackingParameters.Add($"Guide Support Mesh N Vertices", guideSupport.Vertices.Count.ToString());
            TrackingParameters.Add($"Surface Wrap Mesh N Triangles", guideSurfaceWrap.Faces.Count.ToString());
            TrackingParameters.Add($"Surface Wrap Mesh N Vertices", guideSurfaceWrap.Vertices.Count.ToString());

            guideProcessingInputMeshesInfo.ForEach(x =>
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"    > {x.GuideName} region of Guide Support has {x.GuideSupportTriangleCount} triangles");
                IDSPluginHelper.WriteLine(LogCategory.Default, $"    > {x.GuideName} region of Guide Surface Wrap has {x.GuideSurfaceWrapTriangleCount} triangles");

                TrackingParameters.Add($"{x.GuideName} Guide Support Mesh RoI N Triangles", x.GuideSupportTriangleCount.ToString());
                TrackingParameters.Add($"{x.GuideName} Guide Support Mesh RoI N Vertices", x.GuideSupportVertexCount.ToString());
                TrackingParameters.Add($"{x.GuideName} Surface Wrap Mesh RoI N Triangles", x.GuideSurfaceWrapTriangleCount.ToString());
                TrackingParameters.Add($"{x.GuideName} Surface Wrap Mesh RoI N Vertices", x.GuideSurfaceWrapVertexCount.ToString());
            });

            if (failedGuidesInfo.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, " > Failed Guides");
            }
            failedGuidesInfo.ForEach(x =>
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"    > {x}");
            });
        }

        private void GenerateGuidePreviews(RhinoDoc doc, CMFImplantDirector director, CMFObjectManager objectManager,
            ref Dictionary<string, TimeSpan> guidesAndProcessingTimes, ref List<GuideCreatorV2.InputMeshesInfo> guideProcessingInputMeshesInfo,
            ref List<string> failedGuidesInfo)
        {
            var commandHelper = new GuideCreationCommandHelper(director);

            var guidePreviewCreationParams = commandHelper.GetGuidePreviewCreationParams(ref guidesAndProcessingTimes);

            foreach (var guidePreviewCreationParam in guidePreviewCreationParams)
            {
                GenerateGuidePreview(doc, director, objectManager, guidePreviewCreationParam.CaseData, ref guidesAndProcessingTimes,
                    ref guideProcessingInputMeshesInfo, ref failedGuidesInfo, guidePreviewCreationParam.GuideCreationParams, guidePreviewCreationParam.RequiresRegeneration);

                guidePreviewCreationParam.Dispose();
            }
        }

        private void GenerateGuidePreview(RhinoDoc doc, CMFImplantDirector director, CMFObjectManager objectManager, GuidePreferenceDataModel guidePreferenceDataModel,
            ref Dictionary<string, TimeSpan> guidesAndProcessingTimes, ref List<GuideCreatorV2.InputMeshesInfo> guideProcessingInputMeshesInfo,
            ref List<string> failedGuidesInfo, GuideCreatorHelper.CreateGuideParameters guideCreationParameters, bool requiresRegeneration)
        {
            var guideName = GuideCreationCommandHelper.GetGuideNameForReporting(guidePreferenceDataModel);

            TrackGuideCreationParameterInfo(guideName, guidePreferenceDataModel, guideCreationParameters, objectManager);

            var guidePreviewEibb = GuideCreationCommandHelper.GetGuidePreviewEBlock(guidePreferenceDataModel);
            var hasBuildingBlock = objectManager.HasBuildingBlock(guidePreviewEibb);
            if (hasBuildingBlock && !requiresRegeneration)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"{guideName} Preview creation skipped because it is already created.");
                TrackingParameters.Add($"{guideName} Creation", "Skipped");
                _skippedGuides.Add(new KeyValuePair<string, string>(guideName, guidePreferenceDataModel.GuidePrefData.GuideTypeValue));
                return;
            }
            else if (hasBuildingBlock && requiresRegeneration)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"{guideName} Preview will be regenerated.");
                objectManager.DeleteObject(objectManager.GetBuildingBlockId(guidePreviewEibb));
            }

            IDSPluginHelper.WriteLine(LogCategory.Default, $"Creating {guideName} Preview");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            GuideCreatorV2.InputMeshesInfo inputMeshInfo;
            Mesh guidePreview;

            try
            {
                bool isNeedManualQprt; //We don't need to do anything yet if QPRT fails
                guidePreview = GuideCreatorHelper.CreateGuide(doc, director, guidePreferenceDataModel, true
                    , guideCreationParameters, out inputMeshInfo, out isNeedManualQprt);
                if (guidePreview == null)
                {
                    failedGuidesInfo.Add(guideName);
                    _unsuccessfulGuides.Add(new KeyValuePair<string, string>(guideName, guidePreferenceDataModel.GuidePrefData.GuideTypeValue));
                    stopwatch.Stop();
                    return;
                }
            }
            catch (Exception e)
            {
                failedGuidesInfo.Add(guideName);
                _unsuccessfulGuides.Add(new KeyValuePair<string, string>(guideName, guidePreferenceDataModel.GuidePrefData.GuideTypeValue));
                stopwatch.Stop();

                if (e is MtlsException exception)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Operation {exception.OperationName} failed to complete.");
                }

                IDSPluginHelper.WriteLine(LogCategory.Error, $"The following unknown exception was thrown. Please report this to the development team.\n{e}");
                Msai.TrackException(e, "CMF");

                return;
            }

            _successfulGuides.Add(new KeyValuePair<string, string>(guideName, guidePreferenceDataModel.GuidePrefData.GuideTypeValue));

            if (guideCreationParameters.GuideBase != null)
            {
                var guideComponent = new GuideCaseComponent();
                var smoothGuideBaseSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.SmoothGuideBaseSurface, guidePreferenceDataModel);
                objectManager.AddNewBuildingBlock(smoothGuideBaseSurfaceEibb, guideCreationParameters.GuideBase);
            }
            objectManager.AddNewBuildingBlock(guidePreviewEibb, guidePreview);

            stopwatch.Stop();
            var timeTaken = stopwatch.Elapsed;
            var totalTime = GuideCreationCommandHelper.AddProcessingTime(guideName, timeTaken, ref guidesAndProcessingTimes);
            IDSPluginHelper.WriteLine(LogCategory.Default, $"{guideName} Preview took {totalTime:mm\\:ss} to create.");
            
            inputMeshInfo.GuideName = guideName;
            guideProcessingInputMeshesInfo.Add(inputMeshInfo);
        }

        private void TrackGuideCreationParameterInfo(string guideName, GuidePreferenceDataModel guidePrefModel,
            GuideCreatorHelper.CreateGuideParameters guideCreationParameters, CMFObjectManager objectManager)
        {
            var guideComponent = new GuideCaseComponent();
            var positiveGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.PositiveGuideDrawings, guidePrefModel);
            var negativeGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.NegativeGuideDrawing, guidePrefModel);
            var linkSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideLinkSurface, guidePrefModel);
            var guideSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideSurface, guidePrefModel);

            var existingPositiveSurfaces = objectManager.GetAllBuildingBlocks(positiveGuideDrawingEibb);
            var existingNegativeSurfaces = objectManager.GetAllBuildingBlocks(negativeGuideDrawingEibb);
            var existingLinkSurfaces = objectManager.GetAllBuildingBlocks(linkSurfaceEibb);
            var existingGuideSurfaces = objectManager.GetAllBuildingBlocks(guideSurfaceEibb);

            TrackingParameters.Add($"{guideName} N Positive Surfaces", existingPositiveSurfaces.Count().ToString());
            TrackingParameters.Add($"{guideName} N Negative Surfaces", existingNegativeSurfaces.Count().ToString());
            TrackingParameters.Add($"{guideName} N Link Surfaces", existingLinkSurfaces.Count().ToString());
            TrackingParameters.Add($"{guideName} N Guide Surfaces", existingGuideSurfaces.Count().ToString());
            TrackingParameters.Add($"{guideName} N Skeleton Surface", 
                guidePrefModel.PositiveSurfaces.Count(x => x.GuideSurfaceData is SkeletonSurface).ToString());
            TrackingParameters.Add($"{guideName} N Positive Patch Surface",
                guidePrefModel.PositiveSurfaces.Count(x =>
                {
                    var surface = x.GuideSurfaceData as PatchSurface;
                    return surface != null && !surface.IsNegative;
                }).ToString());
            TrackingParameters.Add($"{guideName} N Negative Patch Surface",
                guidePrefModel.NegativeSurfaces.Count(x =>
                {
                    var surface = x.GuideSurfaceData as PatchSurface;
                    return surface != null && surface.IsNegative;
                }).ToString());

            TrackingParameters.Add($"{guideName} N Barrels", guideCreationParameters.Barrels.Count.ToString());
            TrackingParameters.Add($"{guideName} N Bridges", guideCreationParameters.Bridges.Count.ToString());
            TrackingParameters.Add($"{guideName} N Flanges", guideCreationParameters.Flanges.Count.ToString());
            TrackingParameters.Add($"{guideName} N Screws", guideCreationParameters.GuideScrews.Count.ToString());
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            var visualizationComponent = new CMFGuidePreviewVisualization();
            visualizationComponent.GenericVisibility(doc);
        }
    }
}
