using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.CommandHelpers;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.Enumerators;
using IDS.CMF.Invalidation;
using IDS.CMF.Operations;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.Importer;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("ACA45A2B-7FD5-472D-967D-3FBB4CE1D114")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant)]
    public class CMFImportImplantSupport : CmfCommandBase
    {
        public CMFImportImplantSupport()
        {
            TheCommand = this;
            VisualizationComponent = new CMFImplantSupportVisualization();
        }
        
        public static CMFImportImplantSupport TheCommand { get; private set; }
        
        public override string EnglishName => CommandEnglishName.CMFImportImplantSupport;
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            if (mode == RunMode.Interactive)
            {
                var message = "Import implant support can result in implant support inputs to be invalidated.\n" +
                              "Error also can happen due to wrong naming convention assigned to the implant support STL(s).\n\n" +
                              "Do you want to save the 3dm file before import operation?\n";

                if (IDSDialogHelper.ShowMessage(message, "Save Before Import Support", ShowMessageButton.YesNo,
                    ShowMessageIcon.Warning, mode, ShowMessageResult.Yes) == ShowMessageResult.Yes)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    RhinoApp.RunScript(RhinoScripts.SaveFile, false);
                    stopwatch.Stop();
                    TrackingParameters.Add("Save Restore Point", $"{Math.Truncate(stopwatch.Elapsed.TotalSeconds)}");
                }
            }

            string[] filePaths = {};
            if (mode == RunMode.Scripted)
            {
                //skip prompts and get file path from command line
                var filePathsRaw = string.Empty;
                var result = RhinoGet.GetString("FilePaths", false, ref filePathsRaw);
                switch (result)
                {
                    case Result.Success:
                        break;
                    case Result.Cancel:
                    case Result.Nothing:
                        return Result.Cancel;
                    default:
                        return Result.Failure;
                }

                // Use "|" as delimiter because Windows not allow name of path contain "|", so it surely split the file path correctly
                filePaths = filePathsRaw.Split('|');
                var invalidFilePathsFound = false;
                foreach (var filePath in filePaths)
                {
                    if (File.Exists(filePath))
                    {
                        continue;
                    }

                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid file path: {filePath}");
                    invalidFilePathsFound = true;
                }

                if (invalidFilePathsFound)
                {
                    return Result.Failure;
                }
            }
            else
            {
                if (!GetMultipleImplantSupport(out filePaths))
                {
                    return Result.Cancel;
                }
            }

            AllScrewGaugesProxy.Instance.IsEnabled = false;
            
            var implantSupportImporter = new ImplantSupportImporter(director);
            if (!implantSupportImporter.ContainsImplantSupportMesh(filePaths))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "No implant support imported.");
                return Result.Failure;
            }

            doc.UndoRecordingEnabled = false;
            
            var invalidator = new ImportImplantSupportInvalidator(director);
            invalidator.SetImplantSupportInputsDependencyGraph();

            var replaced = implantSupportImporter.ImportImplantSupportMesh(filePaths, out var importedImplantSupportsGuid);

            doc.UndoRecordingEnabled = true;

            if (replaced)
            {
                var implantSupportManager = new ImplantSupportManager(objectManager);

                var verifiedImportedImplantSupportsGuid = new List<Guid>();

                foreach (var guid in importedImplantSupportsGuid)
                {
                    var implantSupportRhinoObject = doc.Objects.Find(guid);
                    if (implantSupportRhinoObject == null)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, $"Missing imported implant support {guid}");
                        continue;
                    }

                    verifiedImportedImplantSupportsGuid.Add(guid);
                }

                invalidator.InvalidateDependentImportSupportInputs(verifiedImportedImplantSupportsGuid);

                implantSupportManager.ResetOutdatedImplantSupportsById(verifiedImportedImplantSupportsGuid);

                doc.ClearUndoRecords(true);
                doc.ClearRedoRecords();
            }

            CasePreferencePanel.GetView().InvalidateUI();

            return replaced ? Result.Success : Result.Failure;
        }

        private bool GetMultipleImplantSupport(out string[] filePaths)
        {
            filePaths = StlImporter.SelectStlFiles(true, "Please select all the implant support STLs file");
            return filePaths != null;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Views.Redraw();
        }
    }
}