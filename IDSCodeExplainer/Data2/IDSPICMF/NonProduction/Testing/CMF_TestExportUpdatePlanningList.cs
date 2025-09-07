using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.FileSystem;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Visualization;
using Newtonsoft.Json;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using System.IO;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)

    [System.Runtime.InteropServices.Guid("FF3A44DB-DBA2-4563-BC47-DC06F1FE5777")]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ProPlanImport)]
    public class CMF_TestExportUpdatePlanningList : CmfCommandBase
    {
        public CMF_TestExportUpdatePlanningList()
        {
            Instance = this;
        }

        public static CMF_TestExportUpdatePlanningList Instance { get; private set; }

        public override string EnglishName => "CMF_TestExportUpdatePlanningList";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var inputFilePath = string.Empty;

            if (mode == RunMode.Scripted)
            {
                var result = RhinoGet.GetString("InputFilePath", false, ref inputFilePath);
                if (result != Result.Success || string.IsNullOrEmpty(inputFilePath) || !File.Exists(inputFilePath))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Invalid sppc/mcs file path: {inputFilePath}");
                    return Result.Failure;
                }
            }
            else
            {
                inputFilePath = FileUtilities.GetFileDir("Please select a SPPC/MCS file", "SPPC files (*.sppc)|*.sppc|MCS files (*.mcs)|*.mcs",
                "Invalid file selected or Canceled.");

                if (inputFilePath == string.Empty)
                {
                    return Result.Failure;
                }
            }

            var importCheckboxList = UpdatePlanningHelper.CompileUpdatePlanningList(inputFilePath, doc, new IDSRhinoConsole());

            var directory = DirectoryStructure.GetWorkingDir(doc);
            using (var file = File.CreateText($"{directory}\\UpdatePlanningList.json"))
            {
                var serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, importCheckboxList);
            }

            SystemTools.OpenExplorerInFolder(directory);

            return Result.Success;
        }
    }

#endif
}
