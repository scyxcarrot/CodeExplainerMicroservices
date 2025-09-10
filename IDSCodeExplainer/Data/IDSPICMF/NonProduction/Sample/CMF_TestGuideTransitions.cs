using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Relations;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("6C698B78-52CB-40BA-A141-A2A7208D631E")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideSurfaceWrap)]
    public class CMF_TestGuideTransitions : CommandBase<CMFImplantDirector>
    {
        public CMF_TestGuideTransitions()
        {
            TheCommand = this;
        }
        
        public static CMF_TestGuideTransitions TheCommand { get; private set; }
        
        public override string EnglishName => "CMF_TestGuideTransitions";

        private const double defaultRadius = 0.4;
        private const double defaultGapClosingDistance = 1.0;
        private string _selectedMode = string.Empty;
        private double _selectedTransitionRadius = defaultRadius;
        private double _selectedTransitionGapClosingDistance = defaultGapClosingDistance;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!PhaseChanger.IsAllImplantsAndGuidesAreNumbered(director))
            {
                return Result.Failure;
            }
            
            GenerateGuidePreviews(doc, director, _selectedMode, _selectedTransitionRadius, _selectedTransitionGapClosingDistance);

            return Result.Success;
        }

        private void GenerateGuidePreviews(RhinoDoc doc, CMFImplantDirector director, string selectedMode, double transitionRadius, double transitionGapClosingDistance)
        {
            var objectManager = new CMFObjectManager(director);

            var guidesAndProcessingTimes = new Dictionary<string, TimeSpan>();
            var guideProcessingInputMeshesInfo = new List<GuideCreatorV2.InputMeshesInfo>();
            var failedGuidesInfo = new List<string>();

            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                var guideCreationParams = new GuideCreatorHelper.CreateGuideParameters(director, guidePreferenceDataModel);

                GenerateGuidePreview(doc, director, objectManager, guidePreferenceDataModel,
                    ref guidesAndProcessingTimes, ref guideProcessingInputMeshesInfo, ref failedGuidesInfo, guideCreationParams);

                guideCreationParams.Dispose();
            }

            doc.Views.Redraw();

            var guideSupport = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry;
            var guideSurfaceWrap = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap).Geometry;

            IDSPluginHelper.WriteLine(LogCategory.Default, "Guide Preview Summary");
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Mode: {_selectedMode}");
            IDSPluginHelper.WriteLine(LogCategory.Default, " > Processing Time");
            foreach (var guideAndProcessingTime in guidesAndProcessingTimes.OrderBy(p => p.Key))
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"    > {guideAndProcessingTime.Key} Preview took {guideAndProcessingTime.Value:mm\\:ss} to create.");
            }
            IDSPluginHelper.WriteLine(LogCategory.Default, " > Mesh Sizes");
            IDSPluginHelper.WriteLine(LogCategory.Default, $"    > Entire Guide Support has {guideSupport.Faces.Count} triangles");
            IDSPluginHelper.WriteLine(LogCategory.Default, $"    > Entire Guide Surface Wrap has {guideSurfaceWrap.Faces.Count} triangles");
            guideProcessingInputMeshesInfo.ForEach(x =>
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"    > {x.GuideName} region of Guide Support has {x.GuideSupportTriangleCount} triangles");
                IDSPluginHelper.WriteLine(LogCategory.Default, $"    > {x.GuideName} region of Guide Surface Wrap has {x.GuideSurfaceWrapTriangleCount} triangles");
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

        private void GenerateGuidePreview(RhinoDoc doc, CMFImplantDirector director, CMFObjectManager objectManager, GuidePreferenceDataModel guidePreferenceDataModel,
            ref Dictionary<string, TimeSpan> guidesAndProcessingTimes, ref List<GuideCreatorV2.InputMeshesInfo> guideProcessingInputMeshesInfo,
            ref List<string> failedGuidesInfo, GuideCreatorHelper.CreateGuideParameters guideCreationParameters)
        {
            var guideName = $"Smooth {guidePreferenceDataModel.CaseName}";

            var guidePreviewEibb = GetGuidePreviewEBlock(guidePreferenceDataModel);
            if (objectManager.HasBuildingBlock(guidePreviewEibb))
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"{guideName} Preview creation skipped because it is already created.");
                return;
            }

            IDSPluginHelper.WriteLine(LogCategory.Default, $"Creating {guideName} Preview");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            GuideCreatorV2.InputMeshesInfo inputMeshInfo;
            var guidePreview = GuideCreatorHelper.CreateGuideWithTransitions(doc, director, guidePreferenceDataModel, true, guideCreationParameters, _selectedTransitionRadius, _selectedTransitionGapClosingDistance, out inputMeshInfo);
            if (guidePreview == null)
            {
                failedGuidesInfo.Add(guideName);
                stopwatch.Stop();
                return;
            }

            objectManager.AddNewBuildingBlock(guidePreviewEibb, guidePreview);

            stopwatch.Stop();
            var timeTaken = stopwatch.Elapsed;
            IDSPluginHelper.WriteLine(LogCategory.Default, $"{guideName} Preview took {timeTaken:mm\\:ss} to create.");

            // Write to STL for comparision
            var stlName = $"{director.caseId}_{guideName}_rad{_selectedTransitionRadius.ToString().Replace(".", "-")}_gcd{_selectedTransitionGapClosingDistance.ToString().Replace(".", "-")}.stl";
            var path = Path.Combine(DirectoryStructure.GetWorkingDir(doc), stlName);
            StlUtilities.RhinoMesh2StlBinary(guidePreview, path);

            guidesAndProcessingTimes.Add(guideName, timeTaken);
            inputMeshInfo.GuideName = guideName;
            guideProcessingInputMeshesInfo.Add(inputMeshInfo);
        }

        private ExtendedImplantBuildingBlock GetGuidePreviewEBlock(ICaseData data)
        {
            var guideComponent = new GuideCaseComponent();
            var guidePreviewEibb = guideComponent.GetGuideBuildingBlock(IBB.GuidePreviewSmoothen, data);
            return guidePreviewEibb;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            var visualizationComponent = new CMFGuidePreviewVisualization();
            visualizationComponent.GenericVisibility(doc);
        }
    }

#endif
}
