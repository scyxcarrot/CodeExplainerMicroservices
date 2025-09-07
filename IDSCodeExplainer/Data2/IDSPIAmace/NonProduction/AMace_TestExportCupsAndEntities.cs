using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Common;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.Operations;
using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace IDS.NonProduction.Commands
{

#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("90bc7848-064e-478d-a3b8-940db370ca47")]
    [IDSCommandAttributes(true, DesignPhase.Cup, IBB.Cup)]
    public class AMace_TestExportCupsAndEntities : Command
    {
        static AMace_TestExportCupsAndEntities _instance;
        public AMace_TestExportCupsAndEntities()
        {
            _instance = this;
        }

        ///<summary>The only instance of the AMace_TestExportCupsAndEntities command.</summary>
        public static AMace_TestExportCupsAndEntities Instance => _instance;

        public override string EnglishName => "AMace_TestExportCupsAndEntities";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Please select a folder with all the STLs";
            DialogResult rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                return Result.Cancel;
            }
            var folderPath = Path.GetFullPath(dialog.SelectedPath);

            var director = new ImplantDirector(doc, PlugInInfo.PluginModel);
            ExportCups(new CupType(2, 1, CupDesign.v1), director, folderPath);
            ExportCups(new CupType(2, 2, CupDesign.v1), director, folderPath);
            ExportCups(new CupType(4, 1, CupDesign.v1), director, folderPath);
            ExportCups(new CupType(2, 1, CupDesign.v2), director, folderPath);
            ExportCups(new CupType(3, 1, CupDesign.v2), director, folderPath);
            ExportCups(new CupType(4, 1, CupDesign.v2), director, folderPath);

            return Result.Success;
        }

        private static void ExportCups(CupType cupType, ImplantDirector director, string outputDir)
        {
            const int testCupDiameterMin = (int)Cup.innerDiameterMin;
            const int testCupDiameterMax = (int)Cup.innerDiameterMax;
            const int testCupApertureMin = (int)Cup.apertureAngleMin;
            const int testCupApertureMax = (int)Cup.apertureAngleMax;

            var cup = director.cup;
            cup.cupType = cupType;

            var cupTypeString = $"CupType_{cupType.CupThickness}+{cupType.PorousThickness}_{cupType.CupDesign}";

            var dummy = new List<String>();

            //Reset aperture to check diameter
            cup.apertureAngle = Cup.apertureAngleDefault;

            for (int i = testCupDiameterMin; i <= testCupDiameterMax; ++i)
            {
                cup.innerCupDiameter = i;
                BlockExporter.ExportBuildingBlocks(director, new List<ImplantBuildingBlock>() { BuildingBlocks.Blocks[IBB.Cup] }, outputDir + $"\\ExportedCupDiameterTest\\{cupTypeString}\\", $"Diameter_{i}mm", "", out dummy);
                BlockExporter.ExportBuildingBlocks(director, new List<ImplantBuildingBlock>() { BuildingBlocks.Blocks[IBB.CupReamingEntity] }, outputDir + $"\\ExportedCupDiameterTest\\{cupTypeString}\\", $"Diameter_{i}mm", "", out dummy);
                BlockExporter.ExportBuildingBlocks(director, new List<ImplantBuildingBlock>() { BuildingBlocks.Blocks[IBB.CupPorousLayer] }, outputDir + $"\\ExportedCupDiameterTest\\{cupTypeString}\\", $"Diameter_{i}mm", "", out dummy);
            }
            
            //Reset diameter to check aperture
            cup.innerCupDiameter = Cup.innerDiameterDefault;

            for (int i = testCupApertureMin; i <= testCupApertureMax; ++i)
            {
                cup.apertureAngle = i;

                BlockExporter.ExportBuildingBlocks(director, new List<ImplantBuildingBlock>() { BuildingBlocks.Blocks[IBB.Cup] }, outputDir + $"\\ExportedCupApertureTest\\{cupTypeString}\\", $"CupAperture_{i}", "", out dummy);
                BlockExporter.ExportBuildingBlocks(director, new List<ImplantBuildingBlock>() { BuildingBlocks.Blocks[IBB.CupReamingEntity] }, outputDir + $"\\ExportedCupApertureTest\\{cupTypeString}\\", $"CupAperture_{i}", "", out dummy);
                BlockExporter.ExportBuildingBlocks(director, new List<ImplantBuildingBlock>() { BuildingBlocks.Blocks[IBB.CupPorousLayer] }, outputDir + $"\\ExportedCupApertureTest\\{cupTypeString}\\", $"CupAperture_{i}", "", out dummy);
            }
        }
    }

#endif
}
