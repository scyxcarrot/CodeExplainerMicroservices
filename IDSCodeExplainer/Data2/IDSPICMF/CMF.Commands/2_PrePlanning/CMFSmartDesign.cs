using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
using Newtonsoft.Json.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("510986D1-8CD4-41EB-869D-822BE18C16F3")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning)]
    public class CMFSmartDesign : CMFUpdateAnatomy
    {
        public CMFSmartDesign()
        {
            Instance = this;
            VisualizationComponent = new CMFSmartDesignRecutVisualization();
        }

        public static CMFSmartDesign Instance { get; private set; }

        public override string EnglishName => CommandEnglishName.CMFSmartDesign;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            ISmartDesignRecutModel dataModel = null;

            if (mode == RunMode.Scripted)
            {
                var inputPath = string.Empty;
                var result = RhinoGet.GetString("DataModel", false, ref inputPath);
                if (result != Result.Success || string.IsNullOrEmpty(inputPath) || !File.Exists(inputPath))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid input path: {inputPath}");
                    return Result.Failure;
                }

                if (!ParseDataModelForScriptedMode(inputPath, out dataModel))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Error while parsing {inputPath}");
                    return Result.Failure;
                }
            }
            else
            {
                var result = GetUserInputs(doc, director, out var viewModel);
                if (result != Result.Success)
                {
                    return result;
                }

                dataModel = viewModel.ConvertToDataModel();
            }

            if (!SmartDesignUtilities.SmartDesignOperation(dataModel, director, out var outputPath))
            {
                return Result.Failure;
            }

            if (!SmartDesignUtilities.ProcessSmartDesignOutput(dataModel, director, outputPath))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "No part changed!");
                return Result.Failure;
            }

            // Split Teeth and Nerves does not require registration for import recut
            if (!SmartDesignUtilities.FindPlannedTeethAndNerves(dataModel, director, out var partsToExcludeRegistration))
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Could not find split teeth or nerve in output.");
            }

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Importing SmartDesign recut outputs...");

            var recutImporter = new SmartDesignRecutImporter(director, partsToExcludeRegistration);
            var success = ExecuteOperation(doc, mode, director, outputPath, recutImporter);

            if (success)
            {
                if (!SmartDesignUtilities.AddWedgesAsReferenceEntities(dataModel, director))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "No wedge added/replaced as reference entity!");
                }
            }

            if (success)
            {
                var directoryInfo = new DirectoryInfo(outputPath);
                directoryInfo.Delete(true);
            }

            TrackingParameters.Add($"WedgeOperation", dataModel.WedgeOperation.ToString());

            return success ? Result.Success : Result.Failure;
        }

        private Result GetUserInputs(RhinoDoc doc, CMFImplantDirector director, out IRecutViewModel viewModel)
        {
            var dialog = new SmartDesignRecutDialog() { Topmost = true };
            dialog.Show();

            var getOption = new GetOption();
            getOption.SetCommandPrompt("Choose the recut type and select parts accordingly");

            var select = getOption.AddOption("Select");
            var preSelect = getOption.AddOption("PreSelect");
            var ok = getOption.AddOption("OK");
            var cancel = getOption.AddOption("Cancel");

            getOption.EnableTransparentCommands(false);
            var res = Result.Cancel;
            Exception exception = null;
            var conduit = new SmartDesignRecutConduit();
            doc.Views.Redraw();

            viewModel = null;

            while (true)
            {
                getOption.Get();

                var commandResult = getOption.CommandResult();
                if (commandResult != Result.Success)
                {
                    if (dialog.CancelConfirmation())
                    {
                        res = Result.Cancel;
                        break;
                    }
                }

                var option = getOption.Option();
                if (option == null)
                {
                    continue;
                }

                dialog.IsEnabled = false;

                var optionSelected = option.Index;
                var needBreak = false;
                try
                {
                    if (optionSelected == preSelect)
                    {
                        PreSelectParts(ref dialog.RecutViewModel, director);
                        conduit.SetViewModel(dialog.RecutViewModel);
                        conduit.Enabled = true;
                        doc.Views.Redraw();
                    }
                    else if (optionSelected == select)
                    {
                        conduit.Enabled = false;
                        doc.Views.Redraw();
                        SelectParts(ref dialog.PartSelectionViewModel, doc);
                        conduit.Enabled = true;
                        doc.Views.Redraw();
                    }
                    else if (optionSelected == ok)
                    {
                        res = OnOK(ref dialog.RecutViewModel, out needBreak);
                        if (res == Result.Success)
                        {
                            viewModel = dialog.RecutViewModel;
                        }
                    }
                    else if (optionSelected == cancel)
                    {
                        if (dialog.CancelConfirmation())
                        {
                            res = Result.Cancel;
                            needBreak = true;
                        }
                    }
                    else
                    {
                        res = Result.Failure;
                        needBreak = true;
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                    needBreak = true;
                }

                dialog.IsEnabled = true;

                if (needBreak)
                {
                    break;
                }
            }

            conduit.Enabled = false;
            conduit.CleanUp();
            dialog.ForceClose();

            if (exception != null)
            {
                throw exception;
            }

            return res;
        }

        private void PreSelectParts(ref IRecutViewModel viewModel, CMFImplantDirector director)
        {
            foreach (var partSelection in viewModel.PartSelections.Values)
            {
                FindPreselectedParts(director, partSelection.DefaultPartNames, out var selection,
                    out var selectionName);
                partSelection.SelectedMeshes = selection;
                partSelection.SourcePartNames = new ObservableCollection<string>(selectionName);
            }
        }

        private void FindPreselectedParts(CMFImplantDirector director, List<string> defaultPartNames, 
            out List<Mesh> selectedParts, out List<string> selectedName)
        {
            selectedParts = new List<Mesh>();
            selectedName = new List<string>();
            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);

            foreach (var partName in defaultPartNames)
            {
                // The "|" separator allows us to add in alternative parts for the selection. Check the default value of each recut data model.
                var possibleNames = partName.Split('|');
                foreach (var possibleName in possibleNames)
                {
                    var block = proPlanImportComponent.GetProPlanImportBuildingBlock(possibleName);

                    if (objectManager.HasBuildingBlock(block))
                    {
                        var rhobj = objectManager.GetBuildingBlock(block);
                        selectedParts.Add((Mesh)rhobj.DuplicateGeometry());
                        selectedName.Add(rhobj.Name.Replace(ProPlanImport.ObjectPrefix, string.Empty));
                        break;
                    }

                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Could not find the part: {possibleName}");
                }
            }
        }

        private void SelectParts(ref PartSelectionViewModel viewModel, RhinoDoc doc)
        {
            UnlockGeneralParts(doc);

            List<Mesh> selection;
            List<string> selectionName;
            var commitChanges = SelectParts(doc, viewModel, out selection, out selectionName);

            if (commitChanges)
            {
                if (!viewModel.IsRequired && !selection.Any())
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"No Selection. Existing selection will be discarded.");
                    viewModel.Reset();
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, $"Selected: {selection.Count} Parts");
                    viewModel.Reset();
                    viewModel.SelectedMeshes = selection;
                    viewModel.SourcePartNames = new ObservableCollection<string>(selectionName);
                }
            }
        }

        private Result OnOK(ref IRecutViewModel recutViewModel, out bool canExit)
        {
            canExit = true;

            if (recutViewModel == null)
            {
                System.Windows.MessageBox.Show("Please make sure that all compulsory fields are filled!");
                canExit = false;
                return Result.Failure;
            }

            foreach (var field in recutViewModel.PartSelections.Values)
            {
                if (field.IsRequired && !field.IsSelected())
                {
                    System.Windows.MessageBox.Show($"Please select {field.PartName}!");
                    canExit = false;
                    return Result.Failure;
                }
            }

            if (!recutViewModel.ValidateCustomInputs())
            {
                canExit = false;
                return Result.Failure;
            }

            return Result.Success;
        }

        private void UnlockGeneralParts(RhinoDoc doc)
        {
            foreach (var obj in doc.Objects)
            {
                doc.Objects.Lock(obj.Id, true);
            }

            var originalParts = ProPlanImportUtilities.GetGeneralParts(doc, ProplanBoneType.Original);
            foreach (var rhinoObject in originalParts)
            {
                doc.Objects.Unlock(rhinoObject.Id, true);
            }

            // Unlock Preop parts since this is required for Wedge BSSO operation
            var preopParts = ProPlanImportUtilities.GetGeneralPreopParts(doc);
            foreach (var rhinoObject in preopParts)
            {
                doc.Objects.Unlock(rhinoObject.Id, true);
            }
        }

        private bool SelectParts(RhinoDoc doc, PartSelectionViewModel viewModel, out List<Mesh> selection, out List<string> selectionName)
        {
            var message = $"Select { viewModel.PartName}. "
                + $"[You may select { (viewModel.MultiParts ? "multiple parts" : "1 part")}]. "
                + $"{ (viewModel.IsRequired ? "" : "This is an optional selection. Press ENTER without selection to discard existing selection.")}";

            var selectParts = new GetObject();
            selectParts.SetCommandPrompt(message);
            selectParts.EnablePreSelect(false, false);
            selectParts.EnablePostSelect(true);
            selectParts.AcceptNothing(true);
            selectParts.EnableTransparentCommands(false);

            var commitChanges = false;
            selection = new List<Mesh>();
            selectionName = new List<string>();

            while (true)
            {
                var result = selectParts.GetMultiple(0, viewModel.MultiParts ? 0 : 1);

                if (result == GetResult.Cancel)
                {
                    break;
                }

                if (result == GetResult.Object || result == GetResult.Nothing)
                {
                    var selectedParts = doc.Objects.GetSelectedObjects(false, false).ToList();

                    if (!selectedParts.Any())
                    {
                        if (!viewModel.IsRequired)
                        {
                            commitChanges = true;
                        }
                        break;
                    }

                    foreach (var rhobj in selectedParts)
                    {
                        selection.Add((Mesh)rhobj.DuplicateGeometry());
                        selectionName.Add(rhobj.Name.Replace(ProPlanImport.ObjectPrefix, string.Empty));
                    }
                    commitChanges = true;
                    break;
                }
            }

            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            return commitChanges;
        }

        private bool ParseDataModelForScriptedMode(string inputPath, out ISmartDesignRecutModel dataModel)
        {
            var parsed = true;
            dataModel = null;

            try
            {
                var jsonText = File.ReadAllText(inputPath);
                var deserialisedObject = JObject.Parse(jsonText);
                var recutType = deserialisedObject.Value<string>("RecutType");
                switch (recutType)
                {
                    case SmartDesignOperations.RecutLefort:
                        dataModel = deserialisedObject.ToObject<SmartDesignLefortRecutModel>();
                        break;
                    case SmartDesignOperations.RecutBSSO:
                        dataModel = deserialisedObject.ToObject<SmartDesignBSSORecutModel>();
                        break;
                    case SmartDesignOperations.RecutGenio:
                        dataModel = deserialisedObject.ToObject<SmartDesignGenioRecutModel>();
                        break;
                    case SmartDesignOperations.RecutSplitMax:
                        dataModel = deserialisedObject.ToObject<SmartDesignSplitMaxRecutModel>();
                        break;
                    default:
                        parsed = false;
                        break;
                }
            }
            catch
            {
                parsed = false;
            }

            return parsed;
        }
    }
}