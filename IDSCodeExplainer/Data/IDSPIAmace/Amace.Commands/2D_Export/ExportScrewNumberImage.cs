using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Commands;
using System.IO;

namespace IDS.Commands.Export
{
    [
        System.Runtime.InteropServices.Guid("E9929989-7682-4F15-A8F5-57AF646FAE35"),
        IDSCommandAttributes(false, DesignPhase.ImplantQC | DesignPhase.Export | DesignPhase.Draft, IBB.Screw)
    ]
    public class ExportScrewNumberImage : CommandBase<ImplantDirector>
    {
        public ExportScrewNumberImage()
        {
            Instance = this;
        }

        ///<summary>The only instance of the ExportParameterFile command.</summary>
        public static ExportScrewNumberImage Instance { get; private set; }

        public override string EnglishName => "ExportScrewNumberImage";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            var docType = director.CurrentDesignPhase == DesignPhase.ImplantQC
                ? DocumentType.ImplantQC
                : DocumentType.Export;
            var fileNameAcetabular = $@"{director.Inspector.CaseId}_Implant_Screws_Design_v{director.version:D}_draft{director.draft:D}.png";
            var filepathAcetabular = Path.Combine(DirectoryStructure.GetWorkingDir(director.Document), fileNameAcetabular);
            ScrewExporter.ExportScrewNumberImage(director, filepathAcetabular, CameraView.Acetabular, docType);

            var fileNamePosterolateral = $@"{director.Inspector.CaseId}_Implant_Screws_Posterolateral_v{director.version:D}_draft{director.draft:D}.png";
            var filepathPosterolateral = Path.Combine(DirectoryStructure.GetWorkingDir(director.Document), fileNamePosterolateral);
            ScrewExporter.ExportScrewNumberImage(director, filepathPosterolateral, CameraView.Illium, docType);

            return Result.Success;
        }
    }
}