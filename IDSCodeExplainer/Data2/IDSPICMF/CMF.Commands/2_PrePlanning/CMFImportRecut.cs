using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using Microsoft.WindowsAPICodePack.Dialogs;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.UI;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("07E3F110-530D-4417-98AC-96106EBD69ED")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Planning)]
    public class CMFImportRecut : CMFUpdateAnatomy
    {
        public CMFImportRecut()
        {
            TheCommand = this;
        }

        public static CMFImportRecut TheCommand { get; private set; }

        public override string EnglishName => CommandEnglishName.CMFImportRecut;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var folderPath = string.Empty;
            var objectManager = new CMFObjectManager(director);

            var proceedIfRepositionedForScriptedMode = true;
            var registerOriginalPartForScriptedMode = true;
            var registerPlannedPartForScriptedMode = true;
            if (mode == RunMode.Scripted)
            {
                //skip prompts and get folder path from command line
                var result = RhinoGet.GetString("FolderPath", false, ref folderPath);
                if (result != Result.Success || string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid folder path: {folderPath}");
                    return Result.Failure;
                }

                result = GetRegisterOriginalPartFlagForScriptedMode(ref registerOriginalPartForScriptedMode);
                if (result != Result.Success)
                {
                    return result;
                }

                result = GetProceedIfRepositionedFlagForScriptedMode(ref proceedIfRepositionedForScriptedMode);
                if (result != Result.Success)
                {
                    return result;
                }

                result = GetRegisterPlannedPartFlagForScriptedMode(ref registerPlannedPartForScriptedMode);
                if (result != Result.Success)
                {
                    return result;
                }
            }
            else
            {
                var response = GetFolderDir(out folderPath);
                if (!response)
                {
                    return Result.Failure;
                }
            }

            var implantSupportImporter = new ImplantSupportImporter(director);
            var stlFilePaths = implantSupportImporter.GetAllStlFilePaths(folderPath);
            var containsImplantSupportMesh = implantSupportImporter.ContainsImplantSupportMesh(stlFilePaths);

            if (mode == RunMode.Interactive)
            {
                if (objectManager.HasBuildingBlock(IBB.ImplantMargin) || objectManager.HasBuildingBlock(IBB.ImplantSupportTeethIntegrationRoI) || objectManager.HasBuildingBlock(IBB.ImplantSupport) ||
                    objectManager.HasBuildingBlock(IBB.ImplantSupportRemovedMetalIntegrationRoI) || objectManager.HasBuildingBlock(IBB.ImplantSupportRemainedMetalIntegrationRoI))
                {
                    var message = "Existing implant support input(s) will be invalidated.\n" +
                                  "Do you want to proceed?";

                    var configMessageResult = Dialogs.ShowMessage(message, "Existing Implant Support Input(s) Found", ShowMessageButton.YesNo, ShowMessageIcon.Warning);
                    if (configMessageResult != ShowMessageResult.Yes)
                    {
                        return Result.Cancel;
                    }
                }

                var messageSave = "Import Recut can result in implant support inputs to be invalidated.\n" +
                                  "Do you want to save the 3dm file before import operation?\n";

                if (IDSDialogHelper.ShowMessage(messageSave, "Save Before Import Recut", ShowMessageButton.YesNo,
                    ShowMessageIcon.Warning, mode, ShowMessageResult.Yes) == ShowMessageResult.Yes)
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    RhinoApp.RunScript(RhinoScripts.SaveFile, false);
                    stopwatch.Stop();
                    TrackingParameters.Add("Save Restore Point", $"{Math.Truncate(stopwatch.Elapsed.TotalSeconds)}");
                }
            }

            var timer = new Stopwatch();
            timer.Start();

            var recutImporter = new RecutImporter(director, mode == RunMode.Interactive, proceedIfRepositionedForScriptedMode, registerOriginalPartForScriptedMode, registerPlannedPartForScriptedMode);
            var success = ExecuteOperation(doc, mode, director, folderPath, recutImporter);

            if (!success)
            {
                timer.Stop();
                return Result.Failure;
            }

            timer.Stop();
            IDSPluginHelper.WriteLine(LogCategory.Default, "Time spent ImportRecut " +
                 $"{ (timer.ElapsedMilliseconds * 0.001).ToString(CultureInfo.InvariantCulture) } seconds");

            return Result.Success;
        }

        private bool GetFolderDir(out string folderPath)
        {
            folderPath = string.Empty;
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Please select a folder with all the STLs";
            dlg.IsFolderPicker = true;
            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            var result = dlg.ShowDialog();
            
            if(result != CommonFileDialogResult.Ok)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Invalid folder or Canceled. No recut part / osteotomy plane / support mesh imported.");
                return false;
            }

            folderPath = Path.GetFullPath(dlg.FileName);
            return true;
        }
    }
}