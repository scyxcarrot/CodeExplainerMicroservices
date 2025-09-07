using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Utilities;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using RhinoMtlsCore.Operations;
using System;
using System.Diagnostics;
using MessageBox = System.Windows.MessageBox;
using Style = Rhino.Commands.Style;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("EDBECE8F-CF43-4314-B761-8ABFCF3E8401")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideSupportRoI)]
    public class CMFCreateGuideSupport : CMFCreateSupportBase
    {
        public CMFCreateGuideSupport()
        {
            TheCommand = this;
            VisualizationComponent = new CMFCreateGuideSupportVisualizationComponent();
        }

        public static CMFCreateGuideSupport TheCommand { get; private set; }

        public override string EnglishName => "CMFCreateGuideSupport";

        public const double MinGCDForGuideSupportRoI = 1.0;
        public const double MaxGCDForGuideSupportRoI = 8.0;
        public const bool DefaultSW2ForGuideSupportRoI = false;

        public const double DefaultSDForGuideSupportRoI = 0.2; //temporary for testing
        public const double MinSDForGuideSupportRoI = 0.2;
        public const double MaxSDForGuideSupportRoI = 10.0;

        public const string SupportMeshType = "Guide Support";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var dataModel = director.GuideManager.GetGuideSupportCreationDataModel();

            var objectManager = new CMFObjectManager(director);

            dataModel.InputRoI = (Mesh) objectManager.GetBuildingBlock(IBB.GuideSupportRoI).Geometry.Duplicate();

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Change the parameter values and press enter.");
            getOption.AcceptNothing(true);
            getOption.EnableTransparentCommands(false);

            var res = Result.Cancel;
            var conduit = new GuideSupportRoIWrapConduit(dataModel);
            conduit.Enabled = true;

            var currentGCD = dataModel.GapClosingDistanceForWrapRoI1;
            var currentSD = DefaultSDForGuideSupportRoI;
            dataModel.SmallestDetailForWrapUnion = currentSD;
            var currentSW2 = DefaultSW2ForGuideSupportRoI;
            dataModel.SkipWrapRoI2 = currentSW2;
            PreviewRoIWrap1(ref dataModel);

            doc.Views.Redraw();

            while (true)
            {
                getOption.ClearCommandOptions();

                var optionGCD = new OptionDouble(currentGCD, MinGCDForGuideSupportRoI, MaxGCDForGuideSupportRoI);
                var gCDOptionIndex = getOption.AddOptionDouble("GapClosingDistanceForWrapRoI1", ref optionGCD,
                    $"Minimum: {MinGCDForGuideSupportRoI}, Maximum: {MaxGCDForGuideSupportRoI}");

#if (INTERNAL)
                var optionSD = new OptionDouble(currentSD, MinSDForGuideSupportRoI, MaxSDForGuideSupportRoI);
                var sDOptionIndex = getOption.AddOptionDouble("SmallestDetailForWrapUnion", ref optionSD,
                    $"Minimum: {MinSDForGuideSupportRoI}, Maximum: {MaxSDForGuideSupportRoI}");

                var optionSW = new OptionToggle(currentSW2, "False", "True");
                var sWOptionIndex = getOption.AddOptionToggle("SkipWrap2", ref optionSW);
#endif

                var getResult = getOption.Get();
                if (getResult == GetResult.Option)
                {
                    if (getOption.OptionIndex() == gCDOptionIndex)
                    {
                        var selectedGcd = optionGCD.CurrentValue;
                        currentGCD = Math.Round(selectedGcd, 1, MidpointRounding.AwayFromZero);
                        IDSPluginHelper.WriteLine(LogCategory.Default,
                            $"Rounding value from {selectedGcd} to {currentGCD}");
                        dataModel.GapClosingDistanceForWrapRoI1 = currentGCD;
                        PreviewRoIWrap1(ref dataModel);
                        doc.Views.Redraw();
                    }
#if (INTERNAL)
                    else if (getOption.OptionIndex() == sDOptionIndex)
                    {
                        currentSD = optionSD.CurrentValue;
                        dataModel.SmallestDetailForWrapUnion = currentSD;
                        doc.Views.Redraw();
                    }
                    else if (getOption.OptionIndex() == sWOptionIndex)
                    {
                        currentSW2 = optionSW.CurrentValue;
                        dataModel.SkipWrapRoI2 = currentSW2;
                        doc.Views.Redraw();
                    }
#endif
                    continue;
                }

                if (getResult == GetResult.Nothing)
                {
                    if (dataModel.WrapRoI1 == null)
                    {
                        MessageBox.Show("Requires RoI Wrap 1 to proceed... Please adjust gap closing distance!");
                        continue;
                    }

                    res = Result.Success;
                    break;
                }
                else if (getResult == GetResult.Cancel || getResult == GetResult.NoResult)
                {
                    break;
                }
            }

            conduit.Enabled = false;

            if (res != Result.Success)
            {
                return res;
            }

            res = GuideSupportCreation(dataModel);
            if (res != Result.Success)
            {
                return res;
            }

            dataModel.FixedFinalResult = PerformFullyFixSupport(dataModel.FinalResult);

            var results = DisplayFinalResultDiagnostics(dataModel.FixedFinalResult);
            ExportIntermediates(doc, dataModel, SupportMeshType);

            return ReplaceSupport(director, dataModel, results);
        }

        private void PreviewRoIWrap1(ref SupportCreationDataModel dataModel)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var creator = new SupportCreator();
            creator.PerformRoIWrap1(ref dataModel);

            stopwatch.Stop();
            AddTrackingParameterSafely("Wrap 1", StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));
        }

        private Result GuideSupportCreation(SupportCreationDataModel dataModel)
        {
            var timer = new Stopwatch();
            timer.Start();

            var creator = new SupportCreator();
            var success = creator.PerformSupportCreation(ref dataModel, out var performanceReport);

            foreach (var keyValuePair in performanceReport)
            {
                AddTrackingParameterSafely(keyValuePair.Key, keyValuePair.Value);
            }

            timer.Stop();

            if (success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic,
                    $"It took {timer.ElapsedMilliseconds * 0.001} seconds to create guide support. (*Note: Time taken only includes automation steps)");
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Failed to create guide support. Please try again later...");
            }

            TrackingParameters.Add("InputRoI N Triangles", dataModel.InputRoI?.Faces.Count.ToString());
            TrackingParameters.Add("InputRoI N Vertices", dataModel.InputRoI?.Vertices.Count.ToString());
            TrackingParameters.Add("FinalResult N Triangles", dataModel.FinalResult?.Faces.Count.ToString());
            TrackingParameters.Add("FinalResult N Vertices", dataModel.FinalResult?.Vertices.Count.ToString());
            TrackingParameters.Add("GapClosingDistanceForWrapRoI1", dataModel.GapClosingDistanceForWrapRoI1.ToString());
            TrackingParameters.Add("SkipWrapRoI2", dataModel.SkipWrapRoI2.ToString());
            TrackingParameters.Add("SmallestDetailForWrapUnion", dataModel.SmallestDetailForWrapUnion.ToString());
            TrackingParameters.Add("Automation Steps Time In Seconds", $"{timer.ElapsedMilliseconds * 0.001}");
            TrackingParameters.Add("Result Status", success.ToString());

            //add to document??

            return success ? Result.Success : Result.Failure;
        }

        private Result ReplaceSupport(CMFImplantDirector director, SupportCreationDataModel dataModel,
            MeshDiagnostics.MeshDiagnosticsResult results)
        {
            if (!ValidateQualityOfSupport(director, results, dataModel.FinalResult, dataModel.FixedFinalResult))
            {
                return Result.Failure;
            }

            var failedMessage = "Something went wrong while replacing guide support mesh";
            var replaced = false;
            try
            {
                //replace GuideSupport if result is fully fixed
                var guideSupportReplacement = new GuideSupportReplacement(director);
                replaced = guideSupportReplacement.ReplaceGuideSupport(dataModel.FixedFinalResult, false);

                if (!replaced)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, failedMessage);
                    ExportFixedSupportMesh(director, dataModel);
                }
                else
                {
                    var doc = director.Document;
                    doc.ClearUndoRecords(true);
                    doc.ClearRedoRecords();

                    director.GuideManager.SetGuideSupportCreationInformation(dataModel);
                }
            }
            catch (Exception e)
            {
                ExportFixedSupportMesh(director, dataModel);

                IDSPluginHelper.WriteLine(LogCategory.Error, failedMessage + " due to exception in operation.");
                Msai.TrackException(e, "CMF");
            }

            return replaced ? Result.Success : Result.Failure;
        }

        private void ExportFixedSupportMesh(CMFImplantDirector director, SupportCreationDataModel dataModel)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var exportDir = $@"{workingDir}\GuideSupportMeshGeneration_Failed";

            StlUtilities.RhinoMesh2StlBinary(dataModel.FixedFinalResult, $"{exportDir}\\Failed_Support_Mesh.stl");
            SystemTools.OpenExplorerInFolder(exportDir);
        }

        protected virtual bool ValidateQualityOfSupport(CMFImplantDirector director, MeshDiagnostics.MeshDiagnosticsResult results, Mesh rawSupport, Mesh notFullyFixedSupport)
        {
            var analysisResult = PostSupportCreationHelper.GetAnalysisResult(results);
            
            if (analysisResult == PostSupportCreationHelper.AnalysisResult.BadTriangle)
            {
                ExportForUserAnalysis(director.Document, rawSupport, notFullyFixedSupport);
                return false;
            }

            if (analysisResult == PostSupportCreationHelper.AnalysisResult.OverlappingTriangleOnly)
            {
                var proceedSupportReplacement =
                    GetConfirmationToProceedWithOverlappingTriangle(director, notFullyFixedSupport);

                if (proceedSupportReplacement == ShowMessageResult.No)
                {
                    ExportForUserAnalysis(director.Document, rawSupport, notFullyFixedSupport);
                    return false;
                }
            }
            return true;
        }

        protected ShowMessageResult GetConfirmationToProceedWithOverlappingTriangle(CMFImplantDirector director, Mesh notFullyFixedSupport)
        {
            var overlappingTriangleConduit = new OverlappingTriangleConduit(notFullyFixedSupport)
            {
                Enabled = true
            };
            director.Document.Views.Redraw();

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Please observe the overlapping triangle(Blue sphere = Attention Region; Red triangle with wire frame = overlapping triangle), and press <Enter> to proceed");
            getOption.Get();

            var proceedSupportReplacement = Dialogs.ShowMessage($"Click <Yes> to keep the guide support.\n\nClick <No> to export \"GuideSupportNotFullyFix.stl\" and " +
                                                                    "\"GuideSupportRaw.stl\" to a folder for further analysis.", "Overlapping Triangle Found", 
                                                                    ShowMessageButton.YesNo, ShowMessageIcon.Exclamation);

            overlappingTriangleConduit.Enabled = false;
            overlappingTriangleConduit.CleanUp();

            return proceedSupportReplacement;
        }

        protected void ExportForUserAnalysis(RhinoDoc doc, Mesh rawSupport, Mesh notFullyFixedSupport)
        {
            var workingDir = DirectoryStructure.GetWorkingDir(doc);
            var exportDir = $@"{workingDir}\GuideSupportAnalysis";

            IDSPluginHelper.WriteLine(LogCategory.Warning, $"Guide support is not able to fully fix, and exported to the following folder for further analysis: {exportDir}");

            StlUtilities.RhinoMesh2StlBinary(rawSupport, $"{exportDir}\\GuideSupportRaw.stl");
            StlUtilities.RhinoMesh2StlBinary(notFullyFixedSupport, $"{exportDir}\\GuideSupportNotFullyFix.stl");

            SystemTools.OpenExplorerInFolder(exportDir);
        }
    }
}
