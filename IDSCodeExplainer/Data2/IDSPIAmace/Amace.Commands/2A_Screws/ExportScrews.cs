using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Importer;
using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using System.Linq;

namespace IDS.Amace.Commands
{
    /**
     * Rhino Command to ...
     */

    [System.Runtime.InteropServices.Guid("416D2674-00D1-4F0C-960C-99DF82AFD40A")]
    [IDSCommandAttributes(false, DesignPhase.Screws, IBB.Screw)]
    public class ExportScrews : CommandBase<ImplantDirector>
    {
        public ExportScrews()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static ExportScrews TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "ExportScrews";

        /**
        * RunCommand does .... as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Export directory
            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);

            // Write temp cup
            const string filenameTag = "Temporary";
            var exportBlocks = ExportBuildingBlocks.GetExportBuildingBlockListScrews();
            BlockExporter.ExportBuildingBlocks(director, exportBlocks.Select(block => BuildingBlocks.Blocks[block]).ToList(), workingDir, filenameTag);

            // Get screws
            var screwManager = new ScrewManager(director.Document);
            var caseId = director.Inspector.CaseId;

            // write xml
            var xmlPath = GenericScrewImportExport.ExportMimicsXml(caseId, screwManager.GetAllScrews(), workingDir);

            // Open the folder via a shell script
            var openedFolder = SystemTools.OpenExplorerInFolder(workingDir);
            if (!openedFolder)
            {
                return Result.Failure;
            }

            // write your code here
            RhinoApp.WriteLine("Screws where exported to the following file:");
            RhinoApp.WriteLine("{0}", xmlPath);
            RhinoApp.WriteLine("Drag this file into 3-matic and copy the cylinders to mimics.");
            return Result.Success;
        }
    }
}