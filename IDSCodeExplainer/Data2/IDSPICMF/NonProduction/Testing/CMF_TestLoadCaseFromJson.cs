using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.TestLib;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using View = IDS.CMF.Visualization.View;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)
    [System.Runtime.InteropServices.Guid("C38E4FC7-2826-467F-8A5D-BF6514079B71")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Initialization)]
    public class CMF_TestLoadCaseFromJson : CmfCommandBase
    {
        public CMF_TestLoadCaseFromJson()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            VisualizationComponent = new CMFImportPreopVisualization();
        }

        ///<summary>The one and only instance of this command</summary>
        public static CMF_TestLoadCaseFromJson TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "CMF_TestLoadCaseFromJson";
        
        public override bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            return IDSPluginHelper.CheckIfCommandIsAllowed(this);
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (director == null)
            {
                director = new CMFImplantDirector(doc, PlugInInfo.PluginModel);
                IDSPluginHelper.SetDirector(doc.DocumentId, director);
            }

            var filePath = FileUtilities.GetFileDir("Please select an JSON file", "JSON files (*.json)|*.json", 
                "Invalid file selected or Canceled.No JSON data imported.");
            if (filePath == string.Empty)
            {
                return Result.Failure;
            }

            CasePreferencePanel.OpenPanel();
            var casePrefViewModel = CasePreferencePanel.GetPanelViewModel();
            casePrefViewModel.ClearPanelUI();
            casePrefViewModel.InitializeDirector(director);
            casePrefViewModel.InitializeInformationOnSurgeryUI();

            var workDir = Path.GetDirectoryName(filePath);
            var json = File.ReadAllText(filePath);
            if (!CMFImplantDirectorConverter.CanParseCaseConfig(json))
            {
                return Result.Failure;
            }

            CMFImplantDirectorConverter.ParseToDirector(json, workDir, director);
            director.draft = 1;
            director.version = 1;
            director.InputFiles = new List<string> { filePath };
            director.UpdateComponentVersions();

            IDSPICMFPlugIn.IsCMF = true;
            CasePreferencePanel.SetEnabled(true);

            foreach (var casePreferenceDataModel in director.CasePrefManager.CasePreferences)
            {
                var implantPreferenceControl = new ImplantPreferenceControl(director)
                {
                    ViewModel =
                    {
                        Model = (ImplantPreferenceModel)casePreferenceDataModel
                    }
                };
                CasePreferencePanel.GetView().GetViewModel().ListViewItems.Add(implantPreferenceControl);
                implantPreferenceControl.InitializeCaseRemarksUI();
            }

            foreach (var guidePreferenceDataModel in director.CasePrefManager.GuidePreferences)
            {
                var guidePreferenceControl = new GuidePreferenceControl(director)
                {
                    ViewModel =
                    {
                        Model = (GuidePreferenceModel)guidePreferenceDataModel
                    }
                };
                CasePreferencePanel.GetView().GetViewModel().GuideListViewItems.Add(guidePreferenceControl);
                guidePreferenceControl.InitializeCaseRemarksUI();
            }

            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            // Visualisation
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[View.PerspectiveViewName];
            View.SetIDSDefaults(doc);
            RhinoApp.WriteLine("Successfully Imported json data.");
            RhinoApp.RunScript("CMFStartPlanningPhase", true);
        }


        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            var CasePrefViewModel = CasePreferencePanel.GetPanelViewModel();
            CasePrefViewModel?.ClearPanelUI();

            RhinoApp.WriteLine("Failed to import json data.");
            RhinoDoc.Create(null);
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            var CasePrefViewModel = CasePreferencePanel.GetPanelViewModel();
            CasePrefViewModel?.ClearPanelUI();
        }
    }
#endif
}