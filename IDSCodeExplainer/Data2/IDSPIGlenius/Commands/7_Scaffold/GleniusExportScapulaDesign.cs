using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.Operations;
using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.IO;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("21D85C84-4BF8-4F51-82ED-A89ED0598C5C")]
    [IDSGleniusCommand(DesignPhase.Scaffold, IBB.ScapulaDesign)]
    public class GleniusExportScapulaDesign : CommandBase<GleniusImplantDirector>
    {
        public GleniusExportScapulaDesign()
        {
            Instance = this;
            VisualizationComponent = new ImportExportUndoScapulaDesignVisualization();
        }
        
        public static GleniusExportScapulaDesign Instance { get; private set; }

        public override string EnglishName => "GleniusExportScapulaDesign";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            //[CaseID]_GR_Scapula_Design_Temporary.stl
            var fileInfo = new FileInfo(doc.Path);
            var workingDir = fileInfo.DirectoryName;
            BlockExporter.ExportBuildingBlocks(director, new List<ImplantBuildingBlock> { BuildingBlocks.Blocks[IBB.ScapulaDesign] }, workingDir, "Temporary");
            SystemTools.OpenExplorerInFolder(workingDir);

            return Result.Success;
        }
    }
}