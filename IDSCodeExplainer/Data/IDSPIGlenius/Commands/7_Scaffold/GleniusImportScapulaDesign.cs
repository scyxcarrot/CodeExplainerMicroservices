using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.IO;
using OpenFileDialog = Rhino.UI.OpenFileDialog;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("02687D21-F0AC-433B-83ED-DAC4EF919762")]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScapulaDesign)]
    [CommandStyle(Style.ScriptRunner)]
    public class GleniusImportScapulaDesign : CommandBase<GleniusImplantDirector>
    {
        public GleniusImportScapulaDesign()
        {
            Instance = this;
            VisualizationComponent = new ImportExportUndoScapulaDesignVisualization();
        }
        
        public static GleniusImportScapulaDesign Instance { get; private set; }
        
        public override string EnglishName => "GleniusImportScapulaDesign";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            string filePath;
            var response = GetFilePath(out filePath);
            if (response != Result.Success)
            {
                return response;
            }

            if (!File.Exists(filePath))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid or no file was chosen");
                return Result.Failure;
            }

            Mesh scapulaDesign;
            if (!StlUtilities.StlBinary2RhinoMesh(filePath, out scapulaDesign))
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Something went wrong while reading the STL file");
                return Result.Failure;
            }

            var objectManager = new GleniusObjectManager(director);
            var commandHelper = new ScapulaDesignCommandHelper(objectManager);
            var success = commandHelper.Update(scapulaDesign);

            if (success)
            {
                director.Graph.NotifyBuildingBlockHasChanged(IBB.ScapulaDesign);
            }

            return success ? Result.Success : Result.Failure;
        }

        private Result GetFilePath(out string filePath)
        {
            filePath = string.Empty;
            
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Choose file path input: show dialog or key in.");
            var showDialog = getOption.AddOption("ShowDialog");
            var keyIn = getOption.AddOption("KeyIn");
            getOption.EnableTransparentCommands(false);
            getOption.Get();

            if (getOption.CommandResult() != Result.Success)
            {
                return getOption.CommandResult();
            }

            var option = getOption.Option();
            if (option == null)
            {
                return Result.Failure;
            }
            var optionSelected = option.Index;
            
            if (optionSelected == showDialog)
            {
                var dialog = new OpenFileDialog();
                dialog.Title = "Please select STL file containing mesh";
                dialog.Filter = "STL files (*.stl)|*.stl||";
                dialog.InitialDirectory = Environment.SpecialFolder.Desktop.ToString();
                dialog.ShowDialog();
                filePath = dialog.FileName;
            }
            else if (optionSelected == keyIn)
            {
                var result = RhinoGet.GetString("Key in STL file path", false, ref filePath);
                if (result != Result.Success)
                {
                    return Result.Failure;
                }
            }
            else
            {
                return Result.Failure;
            }
            return Result.Success;
        }
    }
}