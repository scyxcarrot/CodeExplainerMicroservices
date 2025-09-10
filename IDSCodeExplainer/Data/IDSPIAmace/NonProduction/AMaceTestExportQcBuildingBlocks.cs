using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Commands.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("A4583690-B8AC-4F20-8E68-6E5EA80F14A9")]
    [CommandStyle(Style.ScriptRunner)]
    public class AMaceTestExportQcBuildingBlocks : Command
    {
        public AMaceTestExportQcBuildingBlocks()
        {
            TheCommand = this;
        }
        
        public static AMaceTestExportQcBuildingBlocks TheCommand { get; private set; }

        public override string EnglishName => "AMace_TestExportQCBuildingBlocks";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new GetOption();
            go.SetCommandPrompt("Choose Export QC Type.");
            go.AcceptNothing(true);

            const string cupQc = "CupQC";
            const string implantQc = "ImplantQC";
            const string forFinalization = "ForFinalization";
            var exportOptions = new List<string> { cupQc, implantQc, forFinalization };
            go.AddOptionList("ExportQCType", exportOptions, 0);

            var selectedExport = cupQc;
            while (true)
            {
                var res = go.Get();
                if (res == GetResult.Cancel)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Cancelled.");
                    return Result.Cancel;
                }

                if (res == GetResult.Option)
                {
                    selectedExport = exportOptions[go.Option().CurrentListOptionIndex];
                }
                else if (res == GetResult.Nothing)
                {
                    break;
                }
            }

            var dialog = new FolderBrowserDialog
            {
                Description = "Select Destination to Export QC"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var exportBlocks = new List<ImplantBuildingBlock>();

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (selectedExport)
            {
                case cupQc:
                    exportBlocks = ExportBuildingBlocks.GetExportBuildingBlockListCupQc().Select(x => BuildingBlocks.Blocks[x]).ToList();
                    folderPath = Path.Combine(folderPath, $"2_Draft{director.draft:D}_CupQC");
                    break;
                case implantQc:
                    exportBlocks = ExportBuildingBlocks.GetExportBuildingBlockListImplantQc().Select(x => BuildingBlocks.Blocks[x]).ToList();
                    folderPath = Path.Combine(folderPath, $"2_Draft{director.draft:D}_ImplantQC");
                    break;
                case forFinalization:
                    exportBlocks = ExportBuildingBlocks.GetExportBuildingBlockListPostProcessing().Select(x => BuildingBlocks.Blocks[x]).ToList();
                    folderPath = Path.Combine(folderPath, "IDS_For_Finalization");
                    break;
            }

            if (exportBlocks.Any())
            {
                BlockExporter.ExportBuildingBlocks(director, exportBlocks, folderPath);
            }

            return Result.Success;
        }
    }

#endif
}
