using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Common;
using IDS.Core.CommandBase;
using Rhino;
using Rhino.Commands;

namespace IDS.Amace.Commands
{
    [
        System.Runtime.InteropServices.Guid("908F407F-1ECB-4477-8D37-55D541A3314E"),
        IDSCommandAttributes(false, DesignPhase.CupQC | DesignPhase.ImplantQC | DesignPhase.Export | DesignPhase.Draft, IBB.Cup)
    ]
    public class ExportCupPositionImage : CommandBase<ImplantDirector>
    {
        public ExportCupPositionImage()
        {
            Instance = this;
        }

        ///<summary>The only instance of the ExportParameterFile command.</summary>
        public static ExportCupPositionImage Instance { get; private set; }

        public override string EnglishName => "ExportCupPositionImage";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Image
            var filenameWithOverlay =
                $@"{DirectoryStructure.GetWorkingDir(director.Document)}\{director.Inspector.CaseId}_Cup_Position_v{
                    director.version:D}_draft{director.draft:D}.png";
            CupExporter.ExportCupPositionImage(director, filenameWithOverlay, true);

            // STLs to recreate the image in external tool
            CupExporter.ExportCupPositionParts(DirectoryStructure.GetWorkingDir(director.Document), director);

            return Result.Success;
        }
    }
}