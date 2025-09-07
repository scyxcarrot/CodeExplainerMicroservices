using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using IDS.Glenius.FileSystem;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using IDSPIGlenius;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Glenius.Commands
{
    [
     System.Runtime.InteropServices.Guid("A0181BF9-0108-4956-9219-28B3296A62FA"),
     CommandStyle(Style.ScriptRunner)
    ]
    [IDSGleniusCommand(DesignPhase.Initialization)]
    public class ImportGleniusPreopData : CommandBase<GleniusImplantDirector>
    {
        public ImportGleniusPreopData()
        {
            TheCommand = this;
            VisualizationComponent = new ImportPreopVisualization();
        }

        public static ImportGleniusPreopData TheCommand { get; private set; }

        public override string EnglishName => "ImportGleniusPreopData";

        //Special Case for this Command
        public override bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (!IDSPluginHelper.CheckIfCommandIsAllowed(this))
            {
                return false;
            }

            if (director != null)
            {
                return true;
            }

            var freshDirector = new GleniusImplantDirector(doc, PlugInInfo.PluginModel);
            IDSPluginHelper.SetDirector(doc.DocumentId, freshDirector);

            // Check if preop data was not imported before
            if (freshDirector.InputFiles == null || freshDirector.InputFiles.Count <= 0)
            {
                return true;
            }

            IDSPluginHelper.WriteLine(LogCategory.Error, "Preop data already exists. Aborting.");
            return false;
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            string folderPath;
            var response = GetFolderDir(out folderPath);
            if (response != Result.Success)
            {
                return response;
            }

            var workDir = folderPath + "\\Work";

            var checker = new PreopSTLDataChecker();
            var success = checker.CheckDataIsCorrectAndComplete(folderPath);
            if (!success)
            {
                return Result.Failure;
            }
            
            var dataProvider = new PreopDataProvider();
            var fileInfos = dataProvider.GetSTLFileInfos(folderPath);
            var anyFile = fileInfos.First();
            var caseId = $"{anyFile.CaseID}_{anyFile.CaseType}";
            var preopCorFilePath = dataProvider.GetPreopCorFilePath(folderPath, caseId);
            var inputFiles = fileInfos.Select(file => file.FullPath).ToList();

            var importer = new PreopImporter();
            success = importer.ImportData(director, fileInfos);
            if (!success)
            {
                return Result.Failure;
            }

            if (!string.IsNullOrEmpty(preopCorFilePath))
            {
                var corImporter = new PreopCorImporter();
                success = corImporter.ImportData(preopCorFilePath);
                if (!success)
                {
                    return Result.Failure;
                }
                inputFiles.Add(preopCorFilePath);
                director.PreopCor = corImporter.PreopCor;
            }

            director.caseId = caseId;
            director.DefectSide = anyFile.Side.ToUpperInvariant() == "L" ? "left" : "right";

            // Import custom IDS visualisation (Has to be done in a command because it requires runscript)
            var resources = new Resources();
            var displayModeSettingsFile = resources.IdsSettingsFile;
            RhinoApp.RunScript($"-_OptionsImport \"{displayModeSettingsFile}\" AdvDisplay=Yes Display=Yes _Enter", false);

            // Set meta information
            director.draft = 1;
            director.version = 1;
            director.InputFiles = inputFiles;
            director.BlockToKeywordMapping = importer.BlockToKeywordMapping;
            // Update commits
            director.UpdateComponentVersions();

            Directory.CreateDirectory(workDir);
            RhinoApp.RunScript(
                $"-_Save Version=6 \"{workDir}\\{director.caseId}_work_v{director.version:D}_draft{director.draft:D}.3dm\" _Enter", false);

            director.Graph.InvalidateGraph();

            IDSPIGleniusPlugIn.IsGlenius = true;

            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            Visibility.PreoperativeSituation(doc);
            Visualization.View.SetIDSDefaults(doc);
            RhinoApp.WriteLine("Successfully Imported Preop data.");
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            RhinoApp.WriteLine("Could not import Preop data.");
        }

        private Result GetFolderDir(out string folderPath)
        {
            folderPath = string.Empty;

            var dialog = new FolderBrowserDialog
            {
                Description = "Please select a folder with all the STLs"
            };
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid folder or Canceled. No Preop data imported.");
                return Result.Failure;
            }

            folderPath = Path.GetFullPath(dialog.SelectedPath);

            var directoryOk = DirectoryStructure.CheckDirectoryIntegrity(folderPath, new List<string>() { "inputs", "extrainputs", "extra_inputs" }, new List<string>(), new List<string>() { "3dm" });
            return !directoryOk ? Result.Failure : Result.Success;
        }


    }
}