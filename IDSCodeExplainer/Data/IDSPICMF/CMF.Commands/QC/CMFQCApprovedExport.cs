using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.TreeDb.Model;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("15A89FFE-997C-42F5-8852-4E88C3D97B4C")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.MetalQC | DesignPhase.Draft)]
    public class CMFQCApprovedExport : CmfCommandBase
    {
        public override string EnglishName => "CMFQCApprovedExport";
        private const string GuidePlasticEntities = "Guide plastic entities";
        private const string Mxp = "MXP";
        private List<GuidePreferenceDataModel> _needManualQprtOnThisActualGuides;
        private bool _proceedExportGuidePlasticEntities = true;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var qcExporter = new CMFQCExporter(director, DocumentType.ApprovedQC);
            _needManualQprtOnThisActualGuides = new List<GuidePreferenceDataModel>();

            if (!qcExporter.CanPerformExportQC())
            {
                return Result.Failure;
            }

            Msai.TrackOpsEvent($"Begin {EnglishName}", "CMF");
            Msai.PublishToAzure();

            if (!GetConfirmationToProceed(director, mode))
            {
                return Result.Cancel;
            }

            qcExporter.ProceedWithExportMxp = GetConfirmationToExportAdditionalFiles(mode, Mxp);
            AddTrackingParameterSafely("ProceedWithExportMxp", qcExporter.ProceedWithExportMxp.ToString());
            _proceedExportGuidePlasticEntities = GetConfirmationToExportAdditionalFiles(mode, GuidePlasticEntities);
            AddTrackingParameterSafely("ProceedExportGuidePlasticEntities", _proceedExportGuidePlasticEntities.ToString());

            if (director.CurrentDesignPhase == DesignPhase.MetalQC)
            {
                // Save work file (before changing properties)
                RhinoApp.RunScript(RhinoScripts.SaveFile, false);
            }

            var trackingDictionary = new Dictionary<string, string>();

            if (!GenerateMissingActualImplantsAndGuides(director, ref trackingDictionary))
            {
                return Result.Failure;
            }

            foreach (var keyPairValue in trackingDictionary)
            {
                TrackingParameters.Add(keyPairValue.Key, keyPairValue.Value);
            }

            qcExporter.NotifyUserNeedToPerformManualQprtOnThisActualGuide = _needManualQprtOnThisActualGuides;
            qcExporter.ProceedExportGuidePlasticEntities = _proceedExportGuidePlasticEntities;

            var successExport = qcExporter.DoExportQC();

            foreach (var keyPairValue in qcExporter.ProcessingTimes)
            {
                TrackingParameters.Add(keyPairValue.Key, $"{Math.Truncate(keyPairValue.Value.TotalSeconds)}");
            }

            foreach (var keyPairValue in qcExporter.AdditionalTrackingParameters)
            {
                TrackingParameters.Add(keyPairValue.Key, keyPairValue.Value);
            }

            if (successExport)
            {
                // Open the output folder
                SystemTools.OpenExplorerInFolder(qcExporter.OutputDirectory);
                SystemTools.DiscardChanges();
                CloseAppAfterCommandEnds = true;
                return Result.Success;
            }
            else
            {
                return Result.Failure;
            }
        }

        private bool GenerateMissingActualImplantsAndGuides(CMFImplantDirector director, ref Dictionary<string, string> processingTimes)
        {
            var objManager = new CMFObjectManager(director);
            var implantSupportManager = new ImplantSupportManager(objManager);

            var hasImplantSupport = implantSupportManager.CheckAllImplantsHaveImplantSupport(director);
            var hasGuideSurfaceWrap = objManager.HasBuildingBlock(IBB.GuideSurfaceWrap);

            if (hasImplantSupport)
            {
                ShowActualImplantsGenerationWarningMessages(director);
            }

            if (hasGuideSurfaceWrap)
            {
                ShowActualGuidesGenerationWarningMessages(director);
            }

            var actualImplantGenerationOK = true;
            var actualGuideGenerationOK = true;
            var actualGuidePlasticEntitiesGenerationOK = true;

            if (hasImplantSupport)
            {
                if (!GenerateMissingActualImplants(director, ref processingTimes))
                {
                    actualImplantGenerationOK = false;
                }
            }

            if (hasGuideSurfaceWrap)
            {
                if (!GenerateMissingActualGuides(director, ref processingTimes))
                {
                    actualGuideGenerationOK = false;
                }

                if (actualGuideGenerationOK && !GenerateGuidePlasticEntities(director, ref processingTimes))
                {
                    actualGuidePlasticEntitiesGenerationOK = false;
                }
            }

            var timeSpan = SaveFile(director);
            processingTimes.Add("SaveFile", $"{Math.Truncate(timeSpan.TotalSeconds)}");

            return actualImplantGenerationOK && actualGuideGenerationOK && actualGuidePlasticEntitiesGenerationOK;
        }

        private void ShowActualImplantsGenerationWarningMessages(CMFImplantDirector director)
        {
            var implantCreator = new ImplantCreator(director)
            {
                IsCreateActualImplant = true,
                NumberOfTasks = 2,
            };

            var parameter = ImplantCreatorHelper.CreateImplantCreatorParams(director);
            
            if (director.version > 1)
            {
                var missingActualImplantCount = implantCreator.GetNumberOfMissingActualImplant(parameter);
                var message = (missingActualImplantCount > 0) ? $"{missingActualImplantCount} implants will be recreated because changes has been made to the implant." :
                    "No changes that require the implants to be updated have been made. No implants will be recreated.";
                IDSPluginHelper.WriteLine(LogCategory.Warning, message);
            }
        }

        private void ShowActualGuidesGenerationWarningMessages(CMFImplantDirector director)
        {
            if (director.version > 1)
            {
                var missingActualGuideCount = GuideCreatorHelper.GetNumberOfMissingActualGuide(director);
                var message = (missingActualGuideCount > 0) ? $"{missingActualGuideCount} Guides will be recreated because changes has been made to the guide." :
                    "No changes that require the guides to be updated have been made. No guides will be recreated.";
                IDSPluginHelper.WriteLine(LogCategory.Warning, message);
            }
        }

        private bool GetConfirmationToProceed(CMFImplantDirector director, RunMode mode)
        {
            var message = string.Empty;

            var missingSmoothGuideBaseSurfaceCount = GuideCreatorHelper.GetNumberOfMissingSmoothGuideBaseSurface(director);
            if (missingSmoothGuideBaseSurfaceCount > 0)
            {
                message = $"There are {missingSmoothGuideBaseSurfaceCount} missing Smooth Guide Base Surface(s). " +
                    $"To trigger the generation, a re-generation of Guide Preview Smoothen is required in Guide phase.\n";
            }

            if (director.CurrentDesignPhase == DesignPhase.MetalQC || !string.IsNullOrEmpty(message))
            {
                var confirmation = IDSDialogHelper.ShowMessage($"{message}Are you sure you want to proceed?", "QC Approved Export", 
                    ShowMessageButton.YesNo, ShowMessageIcon.Exclamation, mode, ShowMessageResult.Yes);
                if (confirmation == ShowMessageResult.No)
                {
                    return false;
                }
            }

            return true;
        }

        private bool GetConfirmationToExportAdditionalFiles(RunMode mode, string additionalEntities)
        {
            var confirmation = IDSDialogHelper.ShowExportAdditionalEntitiesConfirmationMessage(mode, additionalEntities);
            return confirmation == ShowMessageResult.Yes;
        }

        private bool GenerateMissingActualImplants(CMFImplantDirector director, ref Dictionary<string, string> processingTimes)
        {
            var implantCreator = new ImplantCreator(director)
            {
                IsCreateActualImplant = true,
                NumberOfTasks = 2,
            };

            var parameter = ImplantCreatorHelper.CreateImplantCreatorParams(director);

            var allSuccessful = implantCreator.GenerateMissingActualImplant(parameter);
            UpdateTrackingInfo(implantCreator.TrackingInfo);

            if (!allSuccessful)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Some implant creation failed.");
                if (implantCreator.UnsuccessfulImplants.Any())
                {
                    var message = "Failed implants : " + string.Join(",", implantCreator.UnsuccessfulImplants);
                    IDSPluginHelper.WriteLine(LogCategory.Error, message +"\nPlease try to generate the implant preview(s) as reference to do fixing.");
                }
            }

            var objectManager = new CMFObjectManager(director);
            var implantComponent = new ImplantCaseComponent();
            var document = director.IdsDocument;
            
            SetBuildingBlock(objectManager, implantComponent, implantCreator.GeneratedImplants.Select(x => new KeyValuePair<CasePreferenceDataModel, Mesh>(x.Key, x.Value.FinalImplant)).ToList(), 
                IBB.ActualImplant, document);
            SetBuildingBlock(objectManager, implantComponent, implantCreator.GeneratedImplantsWithoutStampSubtraction, IBB.ActualImplantWithoutStampSubtraction, document);
            SetBuildingBlock(objectManager, implantComponent, implantCreator.GeneratedImplantSurfaces, IBB.ActualImplantSurfaces, document);
            SetBuildingBlock(objectManager, implantComponent, implantCreator.GeneratedImplantsImprintSubtractEntities.Select(x => new KeyValuePair<CasePreferenceDataModel, Mesh>(x.Key, x.Value.Item1)).ToList(), 
                IBB.ActualImplantImprintSubtractEntity, document);
            SetBuildingBlock(objectManager, implantComponent, implantCreator.GeneratedImplantsScrewIndentationSubtractEntities.Select(x => new KeyValuePair<CasePreferenceDataModel, Mesh>(x.Key, x.Value.Item1)).ToList(), 
                IBB.ImplantScrewIndentationSubtractEntity, document);

            var totalTime = 0.0;

            foreach (var x in implantCreator.GeneratedImplants)
            {
                totalTime += x.Value.TotalTime; //generating time
                totalTime += x.Value.FixingTime; //fixing time

                processingTimes.Add($"Generated Connection and Pastille Time {x.Key.CaseName}", x.Value.TotalTime.ToString(CultureInfo.InvariantCulture));
                processingTimes.Add($"Fixing Time {x.Key.CaseName}", x.Value.FixingTime.ToString(CultureInfo.InvariantCulture));
            }

            #region Plastic Entities Tracking
            var plasticEntitiesTrackingParameters = new Dictionary<string, double>();
            foreach (var x in implantCreator.GeneratedImplantsImprintSubtractEntities)
            {
                foreach (var trackedTimeParameters in x.Value.Item2)
                {
                    if (!plasticEntitiesTrackingParameters.ContainsKey(trackedTimeParameters.Key))
                    {
                        plasticEntitiesTrackingParameters.Add(trackedTimeParameters.Key, 0);
                    }

                    plasticEntitiesTrackingParameters[trackedTimeParameters.Key] += trackedTimeParameters.Value;
                }
            }

            foreach (var x in implantCreator.GeneratedImplantsScrewIndentationSubtractEntities)
            {
                foreach (var trackedTimeParameters in x.Value.Item2)
                {
                    if (!plasticEntitiesTrackingParameters.ContainsKey(trackedTimeParameters.Key))
                    {
                        plasticEntitiesTrackingParameters.Add(trackedTimeParameters.Key, 0);
                    }

                    plasticEntitiesTrackingParameters[trackedTimeParameters.Key] += trackedTimeParameters.Value;
                }
            }

            foreach (var plasticEntitiesTrackingParameter in plasticEntitiesTrackingParameters)
            {
                processingTimes.Add(plasticEntitiesTrackingParameter.Key, $"{Math.Truncate(plasticEntitiesTrackingParameter.Value)}");
            }
            #endregion

            processingTimes.Add("MissingActualImplants", string.Join(",", implantCreator.GeneratedImplants.OrderBy(x => x.Key.NCase).Select(x => x.Key.CaseName))); 
            processingTimes.Add("GenerateMissingActualImplants", $"{Math.Truncate(totalTime)}");


            return allSuccessful;
        }

        private bool GenerateMissingActualGuides(CMFImplantDirector director, ref Dictionary<string, string> processingTimes)
        {
            _needManualQprtOnThisActualGuides = new List<GuidePreferenceDataModel>();
            var allSuccessful = true;

            var guideComponent = new GuideCaseComponent();
            var objectManager = new CMFObjectManager(director);
            var generatedGuides = new List<GuidePreferenceDataModel>();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var guidePreference in director.CasePrefManager.GuidePreferences)
            {
                if (!GuideCreatorHelper.IsActualGuideMissing(director, guidePreference))
                {
                    continue;
                }

                var actualGuideEibb = guideComponent.GetGuideBuildingBlock(IBB.ActualGuide, guidePreference);

                GuideCreatorV2.InputMeshesInfo actualGuideInputMeshInfo;
                bool isNeedManualQprt;
                var actualGuide = GuideCreatorHelper.CreateActualGuide(director.Document, director, guidePreference, false, out actualGuideInputMeshInfo, out isNeedManualQprt);

                generatedGuides.Add(guidePreference);

                if (actualGuide != null)
                {
                    if (isNeedManualQprt)
                    {
                        _needManualQprtOnThisActualGuides.Add(guidePreference);
                    }

                    objectManager.AddNewBuildingBlock(actualGuideEibb, actualGuide);
                }
                else
                {
                    allSuccessful = false;
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Actual guide for {guidePreference.CaseName} failed to generate!");
                }
            }

            stopwatch.Stop();
            
            processingTimes.Add("MissingActualGuides", string.Join(",", generatedGuides.OrderBy(x => x.NCase).Select(x => x.CaseName))); 
            processingTimes.Add("GenerateMissingActualGuides", $"{Math.Truncate(stopwatch.Elapsed.TotalSeconds)}");

            return allSuccessful;
        }
        
        private bool GenerateGuidePlasticEntities(CMFImplantDirector director, ref Dictionary<string, string> processingTimes)
        {
            if (!_proceedExportGuidePlasticEntities)
            {
                return true;
            }

            var allSuccessful = true;
            var allTimeTrackingParameters = new Dictionary<string, double>();

            foreach (var guidePreference in director.CasePrefManager.GuidePreferences)
            {
                var singleTimeTrackingParameters = new Dictionary<string, double>();
                allSuccessful &= PlasticEntitiesCreatorUtilities.GenerateGuidePlasticEntities(director, guidePreference, 
                    ref singleTimeTrackingParameters, out _, out _);

                #region Plastic Entities Tracking
                // It will easy to change if we want to track the time for individual case in future
                foreach (var timeTrackingParameter in singleTimeTrackingParameters)
                {
                    if (!allTimeTrackingParameters.ContainsKey(timeTrackingParameter.Key))
                    {
                        allTimeTrackingParameters.Add(timeTrackingParameter.Key, 0);
                    }

                    allTimeTrackingParameters[timeTrackingParameter.Key] += timeTrackingParameter.Value;
                }
                #endregion
            }

            #region Plastic Entities Tracking
            foreach (var timeTrackingParameter in allTimeTrackingParameters)
            {
                processingTimes.Add(timeTrackingParameter.Key, $"{Math.Truncate(timeTrackingParameter.Value)}");
            }
            #endregion

            return allSuccessful;
        }

        private void SetBuildingBlock(CMFObjectManager objectManager, ImplantCaseComponent implantComponent, 
            List<KeyValuePair<CasePreferenceDataModel, Mesh>> keyValuePairList, IBB block, IDSDocument document)
        {
            foreach (var keyValuePair in keyValuePairList)
            {
                var casePreferenceData = keyValuePair.Key;
                var mesh = keyValuePair.Value;

                var parentBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ImplantPreview, casePreferenceData);
                var parentGuid = objectManager.GetBuildingBlockId(parentBuildingBlock);
                var buildingBlock = implantComponent.GetImplantBuildingBlock(block, casePreferenceData);
                IdsDocumentUtilities.AddNewGeometryBaseBuildingBlock(objectManager, document, buildingBlock, parentGuid, mesh);
            }
        }

        private TimeSpan SaveFile(CMFImplantDirector director)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (director.Document.Modified)
            {
                var currentDocPath = director.Document.Path;
                var fileAttr = File.GetAttributes(currentDocPath);
                if (fileAttr == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(currentDocPath, FileAttributes.Normal);
                }

                RhinoApp.RunScript(RhinoScripts.SaveFile, false);

                if (fileAttr == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(currentDocPath, fileAttr);
                }
            }

            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
    }
}
