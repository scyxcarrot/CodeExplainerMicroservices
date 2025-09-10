using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Importer;
using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using IDS.Glenius.FileSystem;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("EF36AC65-0465-464A-A1FE-BFFD3F791FA7")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Screw)]
    public class GleniusExportScrews : CommandBase<GleniusImplantDirector>
    {
        public GleniusExportScrews()
        {
            TheCommand = this;
        }

        public static GleniusExportScrews TheCommand { get; private set; }


        public override string EnglishName => "GleniusExportScrews";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            // Export directory
            var workingDir = DirectoryStructure.GetWorkingDir(director.Document);

            // Get screws
            var screwManager = director.ScrewObjectManager;
            var caseID = director.caseId;

            //Export Stls & Screws
            var failedStlItems = ExportStls(director, doc, workingDir);
            var failedScrewItems = ExportScrews(director, doc, workingDir);

            failedStlItems.ForEach(x => RhinoApp.WriteLine($"Failed to export {x}"));
            failedScrewItems.ForEach(x => RhinoApp.WriteLine($"Failed to export {x}"));

            // write xml
            var xmlPath = GenericScrewImportExport.ExportMimicsXml(caseID, screwManager.GetAllScrews(), workingDir);

            // Open the folder via a shell script
            var openedFolder = SystemTools.OpenExplorerInFolder(workingDir);
            if (!openedFolder)
            {
                return Result.Failure;
            }

            RhinoApp.WriteLine("Screws where exported to the following file:");
            RhinoApp.WriteLine("{0}", xmlPath);
            RhinoApp.WriteLine("Drag this file into 3-matic and copy the cylinders to mimics.");

            return Result.Success;
        }

        //Returns empty string if everything succeeded
        private List<string> ExportStls(GleniusImplantDirector director, RhinoDoc document, string exportPath)
        {
            var exporter = new ObjectExporter(document);
            exporter.ExportDirectory = exportPath;

            var objManager = new GleniusObjectManager(director);

            //Export Step files
            var exportStp = new Func<string, IBB, bool>((name, ibb) =>
            {
                var brep = objManager.GetBuildingBlock(ibb).Geometry as Brep;
                return exporter.ExportStpAsStl(brep, $"{director.caseId}_{name}_Temporary");
            });

            exportStp(BuildingBlocks.Blocks[IBB.CylinderHat].ExportName, IBB.CylinderHat);
            exportStp(BuildingBlocks.Blocks[IBB.TaperMantleSafetyZone].ExportName, IBB.TaperMantleSafetyZone);
            exportStp(BuildingBlocks.Blocks[IBB.ProductionRod].ExportName, IBB.ProductionRod);

            //Scapula
            var derivedEntities = new ImplantDerivedEntities(director);
            var scapulaReamed0Dot3MmOffsetMesh = derivedEntities.GetScapulaReamedWithWrap();
            exporter.ExportStl(scapulaReamed0Dot3MmOffsetMesh, $"{director.caseId}_Scapula_Reamed_0dot30offset_Temporary");

            return exporter.FailedExports;
        }

        //Returns empty string if everything succeeded
        private List<string> ExportScrews(GleniusImplantDirector director, RhinoDoc document, string exportPath)
        {
            var exporter = new ObjectExporter(document)
            {
                ExportDirectory = exportPath
            };

            //Export Screws and aides
            var screws = director.ScrewObjectManager.GetAllScrews();
            foreach (var s in screws)
            {
                var screwAideGetter = new Func<ScrewAideType, Brep>(
                    aideType => director.Document.Objects.Find(s.ScrewAides[aideType]).Geometry as Brep);

                var safetyZone = screwAideGetter(ScrewAideType.SafetyZone);
                var screwMantle = screwAideGetter(ScrewAideType.Mantle);
                var drillGuideCyl = screwAideGetter(ScrewAideType.GuideDrillCylinder);

                exporter.ExportStpAsStl(s.Geometry as Brep, $"{director.caseId}_Screw{s.Index}_Temporary");
                exporter.ExportStpAsStl(safetyZone, $"{director.caseId}_Screw{s.Index}_Safety_Temporary");
                exporter.ExportStpAsStl(screwMantle, $"{director.caseId}_Screw{s.Index}_Mantle_Temporary");
                exporter.ExportStpAsStl(drillGuideCyl, $"{director.caseId}_Screw{s.Index}_Guide_Drill_Cylinder_Temporary");
            }

            return exporter.FailedExports;
        }

    }
}