using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.DataTypes;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Utilities;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("7C89BDC7-12A6-436A-9AB6-9458709A45B4")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.PlanningImplant)]
    public class CMFCreateImplantSupport : CMFCreateSupportBase
    {
        public CMFCreateImplantSupport()
        {
            TheCommand = this; 
            VisualizationComponent = new CMFImplantSupportVisualization();
        }

        public static CMFCreateImplantSupport TheCommand { get; private set; }

        public override string EnglishName => CommandEnglishName.CMFCreateImplantSupport;
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!GetConfirmationToProceed(mode))
            {
                return Result.Cancel;
            }

            if (!CheckCanProceedWithCreation(director))
            {
                return Result.Cancel;
            }

            var timer = new Stopwatch();
            timer.Start();
            
            var trackingReport = new Dictionary<string, string>();
            var allSupportCreationSuccess = CreateAllMissingOrInvalidateImplantSupports(director, ref trackingReport, out var fixedSupportDataModelsWithCase);
            
            timer.Stop();

            foreach (var keyValuePair in trackingReport)
            {
                AddTrackingParameterSafely(keyValuePair.Key, keyValuePair.Value);
            }

            AddTrackingParameterSafely("Total Time In Seconds", $"{ timer.ElapsedMilliseconds * 0.001}");

            if (!allSupportCreationSuccess)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to create some implant support. Please try again later...");
            }

            var postImplantSupportCreator = new PostImplantSupportCreationHelper(director, fixedSupportDataModelsWithCase);
            
            // Fixed all implant support and mark it for export or replace
            foreach (var fixedSupportDataModelWithCase in fixedSupportDataModelsWithCase)
            {
                var casePref = fixedSupportDataModelWithCase.Key;
                var dataModel = fixedSupportDataModelWithCase.Value;

                dataModel.FixedFinalResult = PerformFullyFixImplantSupport(dataModel.FinalResult, casePref); 
                var results = DisplayFinalResultDiagnostics(dataModel.FixedFinalResult);
                postImplantSupportCreator.AssignAnalysisResult(casePref, results);
            }

            // Show all overlapping triangle and get confirmation for export overlapping triangle once 
            postImplantSupportCreator.ConfirmationToExportOverlappingTriangle();
            postImplantSupportCreator.CategorizeMeshes(out var exportSupportCreationData, out var replaceSupportCreationData);

            // Replace all the good implant support. If failed to replace, include into the list of support for export
            var implantSupportManager = new ImplantSupportManager(new CMFObjectManager(director));
            foreach (var supportCreationDataModel in replaceSupportCreationData)
            {
                var casePref = supportCreationDataModel.Key;
                var dataModel = supportCreationDataModel.Value;
                if (ReplaceSupport(director, casePref, dataModel, out var importedImplantSupportGuid))
                {
                    implantSupportManager.UpdateImplantSupportInputObjectGuidKey(importedImplantSupportGuid);
                }
                else
                {
                    exportSupportCreationData.Add(casePref, dataModel);
                }
            }

            ExportNotFullyFixedSupportMesh(director, exportSupportCreationData);
            replaceSupportCreationData = replaceSupportCreationData.Except(exportSupportCreationData)
                .ToDictionary(kv=>kv.Key, kv=>kv.Value);

            foreach (var performance in trackingReport)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, $"{performance.Key}: {performance.Value}");
            }

            // Summary
            IDSPluginHelper.WriteLine(LogCategory.Default, $"-------------------------Summary-------------------------");
            var replaceSupportsCaseName = replaceSupportCreationData.Keys.Select(c => c.CaseName);
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Success Support: {string.Join(", ", replaceSupportsCaseName)}");

            var exportSupportsCaseName = exportSupportCreationData.Keys.Select(c => c.CaseName);
            IDSPluginHelper.WriteLine(LogCategory.Default, $"Failed Support: {string.Join(", ", exportSupportsCaseName)}");

            IDSPluginHelper.WriteLine(LogCategory.Default, $"Total Time: {StringUtilitiesV2.ElapsedTimeSpanToString(timer.Elapsed)} sec");

            doc.Views.Redraw();
            doc.ClearUndoRecords(true);
            doc.ClearRedoRecords();

            return Result.Success;
        }

        private bool CreateIndividualImplantSupport(CMFImplantDirector director, Mesh biggerRoI,
            CasePreferenceDataModel casePreferenceData, ref Dictionary<string, string> trackingReport, 
            out SupportCreationDataModel dataModel)
        {
            var biggerRoIDuplicated = biggerRoI.DuplicateMesh();
            var creator = new ImplantSupportCreator();
            dataModel = new SupportCreationDataModel();

            var success = creator.PerformIndividualImplantSupportCreation(director, 
                biggerRoIDuplicated, casePreferenceData, trackingReport, ref dataModel);
            return success;
        }

        private bool CreateAllMissingOrInvalidateImplantSupports(CMFImplantDirector director, 
            ref Dictionary<string,string> trackingReport,
            out Dictionary<CasePreferenceDataModel, SupportCreationDataModel> generatedSupportDataModels)
        {
            generatedSupportDataModels = new Dictionary<CasePreferenceDataModel, SupportCreationDataModel>();
            var success = true;

            var objectManager = new CMFObjectManager(director);
            var implantSupportManager = new ImplantSupportManager(objectManager);

            var creator = new ImplantSupportCreator();
            creator.PerformImplantSupportBiggerRoICreation(director, trackingReport, out var biggerRoI);

            foreach (var casePref in director.CasePrefManager.CasePreferences)
            {
                var implantSupport = implantSupportManager.GetImplantSupportMesh(casePref);

                if (implantSupport == null ||
                    OutdatedImplantSupportHelper.IsImplantSupportOutdated(implantSupport))
                {
                    if (CreateIndividualImplantSupport(director, biggerRoI, casePref, ref trackingReport, out var dataModel))
                    {
                        generatedSupportDataModels.Add(casePref, dataModel);
                    }
                    else
                    {
                        success = false;
                    }
                }
            }

            return success;
        }

        private bool CheckCanProceedWithCreation(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            if (!objectManager.HasBuildingBlock(IBB.ImplantMargin))
            {
                var dialogResult = Dialogs.ShowMessage("The file did not contain any margin. Do you want to continue creating implant support?",
                    "Create Implant Support",
                    ShowMessageButton.YesNo,
                    ShowMessageIcon.Exclamation);
                if (dialogResult == ShowMessageResult.No)
                {
                    return false;
                }
            }

            return true;
        }

        private Mesh PerformFullyFixImplantSupport(Mesh rawSupportMesh, CasePreferenceDataModel casePreferenceData)
        {
            var resultantMesh = base.PerformFullyFixSupport(rawSupportMesh);

            var timer = new Stopwatch();
            timer.Start();

            MeshUtilities.RemoveNoiseShellsUsingStatistics(resultantMesh, out var meshesKeep, out var meshesRemove, false, DisjointedShellEditorConstants.AcceptanceNumSigma,
                DisjointedShellEditorConstants.AcceptanceThicknessRatio, DisjointedShellEditorConstants.AcceptanceVolumeHardLimit, DisjointedShellEditorConstants.AcceptanceAreaHardLimit);
            resultantMesh = MeshUtilities.UnionMeshes(meshesKeep);

            timer.Stop();

            IDSPluginHelper.WriteLine(LogCategory.Default, $"It took {timer.ElapsedMilliseconds * 0.001} seconds to removed " +
                                                           $"noise shells from fixed support {casePreferenceData.CaseName}.");
            AddTrackingParameterSafely($"Remove Noise Shells From Fixed Support-{casePreferenceData.CaseName}", StringUtilitiesV2.ElapsedTimeSpanToString(timer.Elapsed));

            return resultantMesh;
        }

        private void ExportNotFullyFixedSupportMesh(CMFImplantDirector director, Dictionary<CasePreferenceDataModel, SupportCreationDataModel> exportSupportCreationData)
        {
            if (!exportSupportCreationData.Any())
            {
                return;
            }

            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);
            var exportDir = $@"{workingDir}\ImplantSupportAnalysis";
            if (Directory.Exists(exportDir))
            {
                SystemTools.DeleteRecursively(exportDir);
            }

            foreach (var supportCreationDataModel in exportSupportCreationData)
            {
                var casePref = supportCreationDataModel.Key;
                var dataModel = supportCreationDataModel.Value;
                StlUtilities.RhinoMesh2StlBinary(dataModel.FinalResult, $"{exportDir}\\ImplantSupport_I{casePref.NCase}_Raw.stl");
                StlUtilities.RhinoMesh2StlBinary(dataModel.FixedFinalResult, $"{exportDir}\\ImplantSupport_I{casePref.NCase}_NotFullyFix.stl");
            }

            SystemTools.OpenExplorerInFolder(exportDir);
        }

        private bool ReplaceSupport(CMFImplantDirector director, CasePreferenceDataModel casePreferenceData, SupportCreationDataModel dataModel, out Guid importedImplantSupportGuid)
        {
            var failedMessage = $"Something went wrong while replacing implant support mesh for {casePreferenceData.CaseName}";
            var replaced = false;
            importedImplantSupportGuid = Guid.Empty;

            try
            {
                var implantSupportReplacement = new ImplantSupportReplacement(director);
                replaced = implantSupportReplacement.ReplaceImplantSupport(casePreferenceData,
                    dataModel.FixedFinalResult, false, out importedImplantSupportGuid);

                if (!replaced)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, failedMessage);
                }
            }
            catch (Exception e)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, failedMessage + $" due to exception in operation. Detail: {e.Message}");
                Msai.TrackException(e, "CMF");
            }

            CasePreferencePanel.GetView().InvalidateUI();

            return replaced;
        }

        private bool GetConfirmationToProceed(RunMode mode)
        {
            var confirmation = IDSDialogHelper.ShowMessage("Are you sure you want to proceed?", "Create Implant Support", 
                ShowMessageButton.YesNo, ShowMessageIcon.Exclamation, mode, ShowMessageResult.Yes);
            return confirmation == ShowMessageResult.Yes;
        }
    }
}