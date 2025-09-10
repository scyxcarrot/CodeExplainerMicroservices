using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.Operations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.GUI;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.Amace.Commands
{
    [
     System.Runtime.InteropServices.Guid("04ea5dcd-a750-4a60-8d0e-bac9bbe76382"),
     CommandStyle(Style.ScriptRunner)
    ]
    [IDSCommandAttributes(false, DesignPhase.Initialization)]
    public class ImportPreopData : CommandBase<ImplantDirector>
    {
        private static frmWaitbar _waitbar;

        private static void ResetWaitBar()
        {
            if (_waitbar != null && !_waitbar.IsDisposed)
            {
                _waitbar.Close();
            }

            _waitbar = new frmWaitbar
            {
                Title = "Importing Preop...",
                FixedStep = 15
            };
        }

        public ImportPreopData()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static ImportPreopData TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "ImportPreopData";
        
        public override bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            return IDSPluginHelper.CheckIfCommandIsAllowed(this);
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            if (director == null)
            {
                director = new ImplantDirector(doc, PlugInInfo.PluginModel);
                IDSPluginHelper.SetDirector(doc.DocumentId, director);
            }

            // Check if preop data was not imported before
            if (director.Inspector != null)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Preop data already exists. Aborting.");
                return Result.Failure;
            }

            // Choose a preop analysis mat file
            var fd = new Rhino.UI.OpenFileDialog
            {
                Title = "Please select a pre-op analysis file",
                Filter = "Preop Analysis Files (*.mat)|*.mat||",
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString()
            };
            var rc = fd.ShowDialog();
            if (rc != System.Windows.Forms.DialogResult.OK)
            {
                IDSPIAmacePlugIn.WriteLine(LogCategory.Error, "Invalid file or Canceled. No Preop data imported.");
                return Result.Failure;
            }
            var matFile = Path.GetFullPath(fd.FileName);

            // Check if the directory can be used to create a new work folder Cannot use
            // DirectoryStructure.GetWorkingDir here, since there is no saved 3dm file yet
            var workingDirParts = matFile.Split('\\').ToList();
            workingDirParts.RemoveAt(workingDirParts.Count - 1);
            var rootDir = string.Join("\\", workingDirParts.ToArray()) + "\\";
            var directoryOk = DirectoryStructure.CheckDirectoryIntegrity(rootDir,
                new List<string>() { "inputs", "extrainputs", "extra_inputs" },
                new List<string>() { Path.GetFileName(matFile) },
                new List<string>() { "3dm", "mat" });
            if (!directoryOk)
            {
                return Result.Failure;
            }

            var workDir = rootDir + "Work\\";

            // Loader
            ResetWaitBar();
            _waitbar.Show();

            // Run CPython mat to pickle converter
            _waitbar.Increment(10, "Convert to pickle file...");
            string pickleFile;
            PreopAnalysisImporter.ConvertPreopMat(matFile, out pickleFile);

            // Import the pickle file as a PreopInspector and assign to director
            _waitbar.Increment(30, "Import pickle file...");
            PreOpInspector inspector;
            var preopImport = new PreopAnalysisImporter();
            var success = preopImport.ImportPythonData(doc, pickleFile, out inspector);
            // Delete imported pickle file
            File.Delete(pickleFile);
            // Add to director
            if (!success)
            {
                return Result.Failure;
            }
            director.Inspector = inspector;

            // Create thickness building blocks
            _waitbar.Increment(40, "Analysis maps...");
            PreopAnalysisImporter.CreateThiBuildingBlocks(director);
            // Create preop meshes building blocks
            _waitbar.Increment(10, "Preop meshes...");
            PreopAnalysisImporter.SetupPreopMeshes(director);
            // Backwards compatibility for Bone Graft Feature (IDS 2.1.0)
            director.ProvideBackwardCompatibilityBoneGraft();

            // Import custom IDS visualisation (Has to be done in a command because it requires runscript)
            var resources = new Resources();
            var displayModeSettingsFile = resources.IdsSettingsFile;
            RhinoApp.RunScript($"-_OptionsImport \"{displayModeSettingsFile}\" AdvDisplay=Yes Display=Yes _Enter", false);

            // Set meta information
            director.draft = 1;
            director.version = 1;
            director.InputFiles = new List<string> { matFile };
            // Update commits
            director.UpdateComponentVersions();

            // Save the project as a work file
            _waitbar.Increment(5, "Creating project...");
            // Make the directory if necessary, automatically checks if it already exists
            Directory.CreateDirectory(workDir);
            // Save
            RhinoApp.RunScript(
                $"-_Save Version=6 \"{workDir}\\{director.Inspector.CaseId}_work_v{director.version:D}_draft{director.draft:D}.3dm\" _Enter", false);

            // Reached the end: success!
            _waitbar.Increment(5);

            IDSPIAmacePlugIn.IsAmace = true;

            return Result.Success;
        }


        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Visualisation
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            Visibility.PreOperativeSituation(doc);
            View.SetIDSDefaults(doc);
            // Clean up & feedback
            _waitbar.Close();
            RhinoApp.WriteLine("Successfully Imported PreOp data.");
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            // Clean up & feedback
            if (_waitbar != null && _waitbar.Visible)
            {
                _waitbar.ReportError("Could not import PreOp data.");
            }
        }
    }
}