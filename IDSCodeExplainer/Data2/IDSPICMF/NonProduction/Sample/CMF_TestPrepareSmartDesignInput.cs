using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
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
using IDS.CMF.FileSystem;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("8B92D08A-DC7B-4071-85B7-5011015500C7")]
    [IDSCMFCommandAttributes(DesignPhase.Implant)]
    public class CMF_TestPrepareSmartDesignInput : CmfCommandBase
    {
        public CMF_TestPrepareSmartDesignInput()
        {
            TheCommand = this;
        }

        public static CMF_TestPrepareSmartDesignInput TheCommand { get; private set; }
        
        public override string EnglishName => "CMF_TestPrepareSmartDesignInput";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var result = GetUserInputs(doc, director, out var viewModel);
            if (result != Result.Success)
            {
                return result;
            }

            var dataModel = viewModel.ConvertToDataModel();
            var workingDir = DirectoryStructure.GetWorkingDir(doc);
            var directory = Path.Combine(workingDir, dataModel.RecutType);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }

            Directory.CreateDirectory(directory);
            return !SmartDesignUtilities.PrepareInputFiles(dataModel, director, directory, out var hasOsteotomyHandler) ? Result.Failure : Result.Success;
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
            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);

            foreach (var partSelection in viewModel.PartSelections.Values)
            {
                var selection = new List<Mesh>();
                var selectionName = new List<string>();

                if (FindPreselectedParts(director, partSelection.DefaultPartNames, out var selectedPartName))
                {
                    var block = proPlanImportComponent.GetProPlanImportBuildingBlock(selectedPartName);
                    var rhobj = objectManager.GetBuildingBlock(block);
                    selection.Add((Mesh)rhobj.DuplicateGeometry());
                    selectionName.Add(rhobj.Name.Replace(ProPlanImport.ObjectPrefix, string.Empty));
                }

                partSelection.SelectedMeshes = selection;
                partSelection.SourcePartNames = new ObservableCollection<string>(selectionName);
            }
        }

        private bool FindPreselectedParts(CMFImplantDirector director, List<string> defaultPartNames, out string selectedPartName)
        {
            selectedPartName = null;
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
                        selectedPartName = possibleName;
                        return true;
                    }

                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Could not find the part: {possibleName}");
                }
            }

            return false;
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

            System.Windows.MessageBox.Show($"Recut type: {recutViewModel.RecutType}");

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
    }
#endif
}