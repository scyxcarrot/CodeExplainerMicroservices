using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.FileSystem;
using IDS.CMF.LogicContext;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Common.Logic;
using IDS.Interface.Loader;
using IDS.Interface.Logic;
using IDS.Interface.Tools;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace IDS.PICMF.LogicContext
{
    public class UIImportPreopsContext : BackEndImportPreopsContext
    {
        private string _filePath;

        public override string FilePath
        {
            get
            {
                const string sppcFileExtension = ".sppc";
                const string mcsFileExtension = ".mcs";

                if (string.IsNullOrEmpty(_filePath))
                {
                    _filePath = FileUtilities.GetFileDir("Please select an SPPC/MCS file",
                        $"SPPC file (*{sppcFileExtension})|*{sppcFileExtension}|" +
                        $"MCS file (*{mcsFileExtension})|*{mcsFileExtension}",
                        "Invalid file selected or Canceled. No Preop data imported.");
                }

                return _filePath;
            }
        }

        public override ConfirmationParameter<ScrewBrandSurgeryParameter> ConfirmationScrewBrandSurgery
        {
            get
            {
                var my = new Initialization();
                new System.Windows.Interop.WindowInteropHelper(my).Owner = RhinoApp.MainWindowHandle();
                my.ShowDialog();

                if (!my.IsEnterPressed && !my.IsImportXmlPressed)
                {
                    IDSPICMFPlugIn.WriteLine(LogCategory.Default, "Load Preop canceled.");
                    return new ConfirmationParameter<ScrewBrandSurgeryParameter>(LogicStatus.Cancel, default);
                }

                if (my.IsImportXmlPressed)
                {
                    base.UpdateScrewBrandSurgery(my.ViewModel.ScrewBrand, my.ViewModel.SurgeryType);
                    if (ImportCasePrefData() != LogicStatus.Success)
                    {
                        return new ConfirmationParameter<ScrewBrandSurgeryParameter>(LogicStatus.Failure, default);
                    }
                }

                TrackingInfo.AddTrackingParameterSafely("Is Import Preset", my.IsImportXmlPressed.ToString());

                return new ConfirmationParameter<ScrewBrandSurgeryParameter>(LogicStatus.Success,
                    new ScrewBrandSurgeryParameter(my.ViewModel.ScrewBrand, my.ViewModel.SurgeryType));
            }
        }

        public UIImportPreopsContext(CMFImplantDirector director, RhinoDoc document, IConsole console) : 
            base(director, document, console)
        {
        }

        public override void ShowErrorMessage(string errorTitle, string errorMessage)
        {
            base.ShowErrorMessage(errorTitle, errorMessage);
            Dialogs.ShowMessage(errorMessage, errorTitle, ShowMessageButton.OK, ShowMessageIcon.Error);
        }

        private LogicStatus ImportCasePrefData()
        {
            string xmlfilePath;
            var responseXML = GetXmlDirectory(out xmlfilePath);
            if (responseXML != LogicStatus.Success)
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Get case preferences xml file failed.");
                return LogicStatus.Failure;
            }

            var importCasePref = new ImportExportCasePreferences();
            var importedCasePref = importCasePref.ImportCasePreferences(xmlfilePath);
            if (importedCasePref == null)
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Imported Case Preferences xml file not match with schema.");
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Import case preferences xml file failed.");
                return LogicStatus.Failure;
            }

            director.ScrewBrandCasePreferences = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(importedCasePref.SurgeryInformation.ScrewBrand);
            director.ScrewLengthsPreferences = CasePreferencesHelper.LoadScrewLengthData();

            CasePreferencePanel.OpenPanel();
            var casePrefView = CasePreferencePanel.GetPanelViewModel();
            casePrefView.ClearPanelUI();
            casePrefView.InitializeDirector(director);
            var infoOnSurgeryControl = casePrefView.InitializeInformationOnSurgeryUI();
            director.CasePrefManager.SurgeryInformation = importedCasePref.SurgeryInformation;

            if (director.CasePrefManager.SurgeryInformation.SurgeryInfoRemarks == null)
            {
                director.CasePrefManager.SurgeryInformation.SurgeryInfoRemarks = ByteUtilities.ConvertRichTextBoxToBytes(new RichTextBox());
            }

            casePrefView.InvalidateInformationOnSurgeryData();
            infoOnSurgeryControl.InitializeCaseRemarksUI();

            foreach (var casePref in importedCasePref.Cases)
            {
                var model = new ImplantPreferenceModel(importedCasePref.SurgeryInformation.SurgeryType, director.ScrewBrandCasePreferences, director.ScrewLengthsPreferences);
                model.AutoUpdateScrewAideOnSelectedScrewTypeChange = false;
                var loadedData = new CasePreferenceDataModel(Guid.NewGuid());
                loadedData.CasePrefData = casePref.CaseData;
                loadedData.CaseName = casePref.CaseName;
                loadedData.NCase = casePref.NCase;
                model.LoadFromData(loadedData);
                model.AutoUpdateScrewAideOnSelectedScrewTypeChange = true;

                if (model.SelectedCaseInfoRemarks == null)
                {
                    model.SelectedCaseInfoRemarks = ByteUtilities.ConvertRichTextBoxToBytes(new RichTextBox());
                }

                director.CasePrefManager.CasePreferences.Add(model);
            }

            var casePrefControls = casePrefView.InitializeCasePreferencesUI(director.CasePrefManager.CasePreferences.Select
                (cp => (ImplantPreferenceModel)cp).ToList());

            casePrefControls.ForEach(x =>
            {
                x.InitializeCaseRemarksUI();
            });

            if (importedCasePref.Guides != null)
            {
                foreach (var guidePref in importedCasePref.Guides)
                {
                    var model = new GuidePreferenceModel(director.ScrewBrandCasePreferences);
                    var loadedData = new GuidePreferenceDataModel(Guid.NewGuid());
                    loadedData.GuidePrefData = guidePref.GuideData;
                    loadedData.CaseName = guidePref.CaseName;
                    loadedData.NCase = guidePref.NCase;
                    model.LoadFromData(loadedData);

                    if (model.SelectedGuideInfoRemarks == null)
                    {
                        model.SelectedGuideInfoRemarks = ByteUtilities.ConvertRichTextBoxToBytes(new RichTextBox());
                    }

                    director.CasePrefManager.GuidePreferences.Add(model);
                }

                var guidePrefControls = casePrefView.InitializeGuidePreferencesUI(director.CasePrefManager.GuidePreferences.Select
                    (gp => (GuidePreferenceModel)gp).ToList());

                guidePrefControls.ForEach(x =>
                {
                    x.InitializeCaseRemarksUI();
                });
            }

            director.CasePrefManager.InitializeGraphs();
            director.CasePrefManager.InitializeEvents();
            return LogicStatus.Success;
        }

        private LogicStatus GetXmlDirectory(out string filePath)
        {
            filePath = string.Empty;

            var dialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = "Please select an JSON file",
                Filter = "JSON files (*.JSON)|*.JSON||",
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString()
            };
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid file selected or Cancelled. No JSON file imported.");
                return LogicStatus.Failure;
            }

            filePath = dialog.FileName;
            return LogicStatus.Success;
        }

        public override void UpdateScrewBrandSurgery(EScrewBrand screwBrand, ESurgeryType surgeryType)
        {
            base.UpdateScrewBrandSurgery(screwBrand, surgeryType);

            CasePreferencePanel.OpenPanel();
            var casePrefViewModel = CasePreferencePanel.GetPanelViewModel();
            casePrefViewModel.ClearPanelUI();
            casePrefViewModel.InitializeDirector(director);
            casePrefViewModel.InitializeInformationOnSurgeryUI();

            // Import custom IDS visualisation
            var resources = new CMFResources();
            var displayModeSettingsFile = resources.IdsCmfSettingsFile;
            DisplayModeDescription.ImportFromFile(displayModeSettingsFile);
        }

        public override bool AskConfirmationToProceed(List<IPreopLoadResult> preLoadData)
        {
            base.AskConfirmationToProceed(preLoadData);

            if (!PartsWithIncompatibleTransformationMatrix.Any() && !PartsFromReferenceObjects.Any())
            {
                return true;
            }

            var proceed = new bool?(true);

            if (PartsWithIncompatibleTransformationMatrix.Any())
            {
                var prompt = new ProPlanTransformationMatrixPrompt
                {
                    Topmost = true
                };

                prompt.SetPartNames(PartsWithIncompatibleTransformationMatrix);
                proceed = prompt.ShowDialog();
            }

            if (proceed == true && PartsFromReferenceObjects.Any())
            {
                var prompt = new EnlightReferenceObjectPrompt
                {
                    Topmost = true
                };

                prompt.SetPartNames(PartsFromReferenceObjects);
                proceed = prompt.ShowDialog();
            }

            return proceed == true;
        }

        public override LogicStatus PostProcessData()
        {
            try
            {
                var status = base.PostProcessData();
                if (status != LogicStatus.Success)
                {
                    return status;
                }

                IDSPICMFPlugIn.IsCMF = true;
                CasePreferencePanel.SetEnabled(true);

                Msai.Terminate(PlugInInfo.PluginModel, director.FileName, IDSPICMFPlugIn.SharedInstance.CaseVersion,
                    IDSPICMFPlugIn.SharedInstance.CaseDraft);
                Msai.Initialize(PlugInInfo.PluginModel, director.FileName, director.version, director.draft);

                IDSPICMFPlugIn.SharedInstance.CaseVersion = director.version;
                IDSPICMFPlugIn.SharedInstance.CaseDraft = director.draft;

                var totalNumTriangles = 0;
                foreach (var docLayer in document.Layers)
                {
                    var objectsInLayer = document.Objects.FindByLayer(docLayer).ToList();
                    var meshesInLayer = objectsInLayer.Where(x => x.Geometry is Mesh).ToList();

                    if (!meshesInLayer.Any())
                    {
                        continue;
                    }

                    var numTriangles = 0;
                    meshesInLayer.ForEach(x =>
                    {
                        numTriangles += ((Mesh)x.Geometry).Faces.Count;
                        totalNumTriangles += numTriangles;
                    });

                    var keyName = $"{docLayer.FullPath} N Triangles";

                    if (!TrackingInfo.AddTrackingParameterSafely(keyName, numTriangles.ToString()))
                    {
                        Msai.TrackException(new Exception($"KEY already exist for {keyName}, DEBUG THIS ASAP!"), "CMF");
                    }
                }

                TrackingInfo.AddTrackingParameterSafely("Import PreOp Meshes Total N Triangles",
                    totalNumTriangles.ToString());
                TrackingInfo.AddTrackingParameterSafely("InputFileExt", Path.GetExtension(FilePath).ToLower());

                UiUtilities.SubscribePanelWidthInvalidation();
            }
            catch (Exception e)
            {
                Msai.Terminate(PlugInInfo.PluginModel, director.FileName, IDSPICMFPlugIn.SharedInstance.CaseVersion,
                    IDSPICMFPlugIn.SharedInstance.CaseDraft);
                //Provide input file name instead since there may not be a 3dm generated
                Msai.Initialize(PlugInInfo.PluginModel, Path.GetFileName(FilePath), director.version, director.draft);
                Msai.TrackException(e, "CMF");

                return LogicStatus.Failure;
            }
            return LogicStatus.Success;
        }
    }
}
