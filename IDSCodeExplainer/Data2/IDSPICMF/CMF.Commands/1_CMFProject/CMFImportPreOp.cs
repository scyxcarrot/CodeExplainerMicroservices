using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.V2.Logics;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Interface.Logic;
using IDS.PICMF.Forms;
using IDS.PICMF.LogicContext;
using IDS.PICMF.Visualization;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.IO;
using System.Linq;
using View = IDS.CMF.Visualization.View;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("6F1E2B9E-B7BF-4120-AC5F-2BAFC0408243")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Initialization)]
    public class CMFImportPreOp : CmfCommandBase
    {
        public CMFImportPreOp()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            VisualizationComponent = new CMFImportPreopVisualization();
        }

        ///<summary>The one and only instance of this command</summary>
        public static CMFImportPreOp TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "CMFImportPreOp";
        
        public override bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            return IDSPluginHelper.CheckIfCommandIsAllowed(this);
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var console = new IDSRhinoConsole();

            if (director == null)
            {
                director = new CMFImplantDirector(doc, PlugInInfo.PluginModel);
                IDSPluginHelper.SetDirector(doc.DocumentId, director);
            }

            // Check if preop data was not imported before
            if (director.InputFiles != null && director.InputFiles.Count > 0)
            {
                console.WriteErrorLine("Preop data already exists. Aborting.");
                return Result.Failure;
            }

            var context = new UIImportPreopsContext(director, doc, console);
            var logic = new ImportPreopsLogic(console);
            var status = logic.Execute(context);

            UpdateTrackingInfo(context.TrackingInfo);

            if (status != LogicStatus.Success)
            {
                console.WriteErrorLine("Load Preop failed.");
            }
            else
            {
                AddInputFileTypeTrackingParameter(director, context.FilePath);
            }

            return status.ToResultStatus();
        }

        private void AddInputFileTypeTrackingParameter(CMFImplantDirector director, string filePath)
        {
            var fileExtension = Path.GetExtension(filePath);
            switch (fileExtension)
            {
                case ".sppc":
                    director.CurrentInputFileType = InputFileType.SppcFile;
                    break;
                case ".mcs":
                    director.CurrentInputFileType = InputFileType.EnlightMcsFile;
                    break;
                default:
                    throw new Exception($"Unrecognized file extension, " +
                                        $"fileExtension = {fileExtension}");
            }

            AddTrackingParameterSafely("Input File Type",
                InputFileTypeConverter.GetStringValue(director.CurrentInputFileType));
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            // Visualisation
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[View.PerspectiveViewName];
            View.SetIDSDefaults(doc);
            RhinoApp.WriteLine("Successfully Imported PreOp data.");
            RhinoApp.RunScript("CMFStartPlanningPhase", true);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            var casePrefViewModel = CasePreferencePanel.GetPanelViewModel();
            casePrefViewModel?.ClearPanelUI();

            RhinoApp.WriteLine("Failed to import PreOp data.");
            RhinoDoc.Create(null);
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            var casePrefViewModel = CasePreferencePanel.GetPanelViewModel();
            casePrefViewModel?.ClearPanelUI();
        }
    }
}