using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using System.Linq;

namespace IDS.Amace.Commands
{
    /**
     * Command to export the design pelvis as an stl file
     */

    [System.Runtime.InteropServices.Guid("175260FF-8394-4618-A22D-820B5A802AC2")]
    [IDSCommandAttributes(false, DesignPhase.Reaming, IBB.DesignPelvis)]
    public class ExportDesignPelvis : CommandBase<ImplantDirector>
    {
        public ExportDesignPelvis()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static ExportDesignPelvis TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "ExportDesignPelvis";

        /**
         * Export the design pelvis as an stl file
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Export directory and filepath
            var workingDirParts = doc.Path.Split('\\').ToList();
            workingDirParts.RemoveAt(workingDirParts.Count - 1);
            var workingDir = string.Join("\\", workingDirParts.ToArray());

            // Write building blocks to files
            var exportBlocks = ExportBuildingBlocks.GetExportBuildingBlockListDesignPelvis();
            BlockExporter.ExportBuildingBlocks(director, exportBlocks.Select(block => BuildingBlocks.Blocks[block]).ToList(), workingDir, "Temporary");

            // Open the folder via a shell script
            var success = SystemTools.OpenExplorerInFolder(workingDir);

            doc.Views.Redraw();

            return success ? Result.Success : Result.Failure;
        }
    }
}