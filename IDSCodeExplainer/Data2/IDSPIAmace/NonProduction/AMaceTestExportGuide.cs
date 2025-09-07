using IDS.Amace;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Commands.NonProduction
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("683B7BC0-9004-499B-8CF9-24A7F719C272")]
    [CommandStyle(Style.ScriptRunner)]
    public class AMaceTestExportGuide : Command
    {
        public AMaceTestExportGuide()
        {
            TheCommand = this;
        }
        
        public static AMaceTestExportGuide TheCommand { get; private set; }

        public override string EnglishName => "AMace_TestExportGuide";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var go = new GetOption();
            go.SetCommandPrompt("Choose Guide.");
            go.AcceptNothing(true);

            const string cupGuide = "Cup";
            const string screwHolePlugsGuide = "ScrewHolePlugs";
            var guideOptions = new List<string> { cupGuide, screwHolePlugsGuide };
            go.AddOptionList("Guide", guideOptions, 0);

            var selectedGuide = cupGuide;
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
                    selectedGuide = guideOptions[go.Option().CurrentListOptionIndex];
                }
                else if (res == GetResult.Nothing)
                {
                    break;
                }
            }

            var dialog = new FolderBrowserDialog
            {
                Description = "Select Destination to Export Guide"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);

            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            var screwManager = new ScrewManager(director.Document);
            var cup = director.cup;
            var screws = screwManager.GetAllScrews().ToList();
            var guideCreator = new GuideCreator(cup, screws);

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (selectedGuide)
            {
                case cupGuide:
                    guideCreator.ExportGuideCupEntity(folderPath, director.Inspector.CaseId, director.version, director.draft);
                    break;
                case screwHolePlugsGuide:
                    var objectManager = new AmaceObjectManager(director);
                    var plateWithoutHoles = objectManager.GetBuildingBlock(IBB.PlateFlat).Geometry as Mesh;
                    guideCreator.ExportGuideScrewHolePlugsEntity(plateWithoutHoles, folderPath, director.Inspector.CaseId, director.version, director.draft);
                    break;
            }

            return Result.Success;
        }
    }

#endif
}
