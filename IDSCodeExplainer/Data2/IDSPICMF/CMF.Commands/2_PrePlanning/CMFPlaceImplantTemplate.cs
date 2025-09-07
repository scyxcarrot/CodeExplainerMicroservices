using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.PICMF.Forms;
using IDS.PICMF.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Style = Rhino.Commands.Style;

namespace IDS.PICMF.Commands
{
    [Guid("B17FBC3E-AFAD-432D-BAD6-F80800B52294")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning)]
    public class CMFPlaceImplantTemplate : CmfCommandBase
    {
        static CMFPlaceImplantTemplate _instance;

        public static CMFPlaceImplantTemplate Instance => _instance;

        public override string EnglishName => "CMFPlaceImplantTemplate";

        public CMFPlaceImplantTemplate()
        {
            _instance = this;
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!GetCasePreferenceId(out var caseId) ||
                !GetImplantType(out var implantType) ||
                !GetImplantTemplateId(out var implantTemplateId))
            {
                return Result.Failure;
            }

            var needInvalidation = NeedInvalidateCasePreferences(director, caseId, out var casePref);
            if (needInvalidation)
            {
                if (MessageBox.Show(
                    "This implant contains an existing planning (and design). " +
                    "Applying a new template will remove the previously created planning (and design) from the document forever. " +
                    "Are you sure you want to proceed?", "Implant Template Overwrite Existing Planning and Design", 
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return Result.Cancel;
                }
            }

            ShowPlanningLayer(director);

            var selectedImplantTemplateDataModel = ImplantTemplateGroupsDataModelManager.FindImplantTemplate(implantType, implantTemplateId);
            var placeImplantTemplate = new PlaceImplantTemplate(director, casePref.ViewModel.Model.CasePrefData, selectedImplantTemplateDataModel);
            var result = placeImplantTemplate.Place(casePref.ViewModel.Model);

            if (result == Result.Success)
            {
                if (needInvalidation)
                {
                    InvalidateCasePreferences(director, casePref);
                }

                casePref.ViewModel.Model.ImplantDataModel = placeImplantTemplate.ImplantDataModel;
                casePref.ViewModel.Model.InvalidateEvents(director);
                director.ImplantManager.HandleAddNewImplant(casePref.ViewModel.Model, false);

                doc.ClearUndoRecords(true);
                doc.ClearRedoRecords();
            }

            doc.Views.Redraw();

            return Result.Success;
        }

        public void ShowPlanningLayer(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var rhObj = objectManager.GetAllBuildingBlocks(IBB.PlanningImplant);

            var rhinoObjects = rhObj.ToList();
            
            rhinoObjects.ForEach(x =>
            {
                var l = x.Attributes.LayerIndex;
                var layer = director.Document.Layers[l];
                layer.CommitChanges();

                if (!layer.IsVisible && layer.IsValid)
                {
                    director.Document.Layers.ForceLayerVisible(layer.Id);
                }
            });

            director.Document.Views.Redraw();
        }

        private string GetCommandString(string prompt)
        {
            var commandString = string.Empty;
            if (RhinoGet.GetString(prompt, false, ref commandString) != Result.Success)
            {
                return null;
            }
            return commandString;
        }

        private bool GetCasePreferenceId(out Guid casePreferenceId)
        {
            casePreferenceId = Guid.Empty;
            var casePreferenceIdStr = GetCommandString("CasePreferenceId");
            if (casePreferenceIdStr == null)
            {
                return false;
            }
            
            if (!Guid.TryParse(casePreferenceIdStr, out casePreferenceId))
            {
                return false;
            }
            return true;
        }

        private bool GetImplantType(out string implantType)
        {
            implantType = string.Empty;
            var implantTypeFromCommand = GetCommandString("ImplantType");
            if (implantType == null)
            {
                return false;
            }

            implantType = implantTypeFromCommand;
            return true;
        }

        private bool GetImplantTemplateId(out string implantTemplateId)
        {
            implantTemplateId = string.Empty;
            var implantTemplateIdFromCommand = GetCommandString("ImplantTemplateId");
            if (implantTemplateIdFromCommand == null)
            {
                return false;
            }

            implantTemplateId = implantTemplateIdFromCommand;
            return true;
        }

        private bool NeedInvalidateCasePreferences(CMFImplantDirector director, Guid caseId, out ImplantPreferenceControl casePref)
        {
            var listViewItems = CasePreferencePanel.GetPanelViewModel().ListViewItems;
            casePref = null;

            foreach (var listViewItem in listViewItems)
            {
                var currentCasePref = listViewItem as ImplantPreferenceControl;
                if (currentCasePref == null || currentCasePref.ViewModel.Model.CaseGuid != caseId)
                {
                    continue;
                }

                casePref = currentCasePref;
                if (!director.CasePrefManager.IsContainCasePreference(currentCasePref.ViewModel.Model))
                {
                    break;
                }

                return true;
            }

            return false;
        }

        private void InvalidateCasePreferences(CMFImplantDirector director, ImplantPreferenceControl casePref)
        {
            ImplantCreationUtilities.DeleteImplantSupportAttributes(director, casePref.ViewModel.Model);

            director.CasePrefManager.HandleDeleteCasePreference(casePref.ViewModel.Model);
            casePref.ViewModel.Model.ImplantDataModel = new ImplantDataModel();
            CasePreferencePanel.GetView().InvalidateUI();
        }
    }
}
