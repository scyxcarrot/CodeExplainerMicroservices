using IDS.Amace;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Core.Enumerators;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino.Geometry;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace IDS.Operations.Export
{
    public static class FileExporter
    {
        /// <summary>
        /// Save the 3dm file for export and tag it as a new version
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="targetDocumentType">Type of the target document.</param>
        /// <param name="filename">The filename.</param>
        /// <exception cref="IDSException">Could not create project draft file.</exception>
        public static void Export3DmProject(ImplantDirector director, DocumentType targetDocumentType, string filename)
        {
            // Write parameter summary to notes
            director.WriteParametersToNotes();
            // Update traceability information for exported 3dm file, discarded afterwards
            director.UpdateComponentVersions();

            // Set the document type as an export document
            var prevDocType = director.documentType;
            director.documentType = targetDocumentType;

            // Save projects as
            var options = new Rhino.FileIO.FileWriteOptions();
            // If a file exists, set read-write access so it can be overwritten
            if (File.Exists(filename))
            {
                File.SetAttributes(filename, System.IO.FileAttributes.Normal);
            }
            // Write the file
            var draftProjectWritten = director.Document.WriteFile(filename, options);
            if (draftProjectWritten)
            {
                File.SetAttributes(filename, System.IO.FileAttributes.ReadOnly);
            }
            else
            {
                throw new IDSException("Could not create project draft file.");
            }

            // Reset project doctype of current file
            director.documentType = prevDocType;

            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done exporting project.");
        }

        /// <summary>
        /// Exports for plastic production.
        /// </summary>
        /// <param name="director">The director.</param>
        public static void ExportForPlasticProduction(ImplantDirector director)
        {
            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Exporting for Plastic Models...");

            // get plastic production output folder
            var plasticProdOutputFolder = DirectoryStructure.GetPlasticProdOutputFolder(director);

            // Make exportable blocks depend on design phase
            var exportedBlocks = ExportBuildingBlocks.GetExportBuildingBlockListPlasticModels()
                .Select(x => BuildingBlocks.Blocks[x]).ToList();

            // For each exported mesh: its mesh, description/name, STL path
            BlockExporter.ExportBuildingBlocks(director, exportedBlocks, plasticProdOutputFolder);

            // Export Acetabular plane
            CupExporter.ExportAcetabularPlane(plasticProdOutputFolder, director);

            // Zip file
            CompressToZipFileWithSameName(plasticProdOutputFolder);

            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done exporting for Plastic Models.");
        }

        /// <summary>
        /// Exports for guide design.
        /// </summary>
        /// <param name="director">The director.</param>
        public static void ExportForGuideDesign(ImplantDirector director)
        {
            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Exporting for Guide Design...");

            // get guide output folder
            var guideOutputFolder = DirectoryStructure.GetGuideOutputFolder(director);

            var screwManager = new ScrewManager(director.Document);
            var objManager = new AmaceObjectManager(director);
            var plateWithoutHoles = objManager.GetBuildingBlock(IBB.PlateFlat).Geometry as Mesh;
            var cup = director.cup;
            var screws = screwManager.GetAllScrews().ToList();
            var guideCreator = new GuideCreator(cup, screws);
            guideCreator.ExportAllGuideEntities(plateWithoutHoles, director.DrillBitRadius, guideOutputFolder, director.Inspector.CaseId, director.version, director.draft);

            // Zip file
            CompressToZipFileWithSameName(guideOutputFolder);

            // Show status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done exporting for Guide Design.");
        }

        /// <summary>
        /// Exports for reporting.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="fullPlateWithTransition">Full rounded plate with transition on Screw Bump and Flange to Cup</param>
        public static void ExportForReporting(ImplantDirector director, Mesh fullPlateWithTransition)
        {
            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Exporting for Reporting...");

            // Get list of building blocks
            var reportingExportBuildingBlocks = ExportBuildingBlocks.GetExportBuildingBlockListReporting()
                .Select(x => BuildingBlocks.Blocks[x]).ToList();

            // get post proc output folder
            var reportingOutputFolder = DirectoryStructure.GetReportingOutputFolder(director);

            // For each exported mesh: its mesh, description/name, STL path
            BlockExporter.ExportBuildingBlocks(director, reportingExportBuildingBlocks, reportingOutputFolder);

            // Parameter file
            var parameterFileName = $"{director.Inspector.CaseId}_Design_Parameters_v{director.version:D}_draft{director.draft:D}.txt";
            var parameterFilePath= Path.Combine(reportingOutputFolder, parameterFileName);
            ParameterExporter.ExportParameterFile(director, parameterFilePath);

            const DocumentType docType = DocumentType.Export;
            // Screw number image (acetabular view)
            var fileNameAcetabular = $@"{director.Inspector.CaseId}_Implant_Screws_Design_v{director.version:D}_draft{director.draft:D}.png";
            var filepathAcetabular = Path.Combine(reportingOutputFolder, fileNameAcetabular);
            ScrewExporter.ExportScrewNumberImage(director, filepathAcetabular, CameraView.Acetabular, docType);
            // Screw number image (posterolateral view)
            var fileNamePosterolateral = $@"{director.Inspector.CaseId}_Implant_Screws_Posterolateral_v{director.version:D}_draft{director.draft:D}.png";
            var filepathPosterolateral = Path.Combine(reportingOutputFolder, fileNamePosterolateral);
            ScrewExporter.ExportScrewNumberImage(director, filepathPosterolateral, CameraView.Illium, docType);

            // Cup position data Image
            var cupPositionImageFileName =$@"{director.Inspector.CaseId}_Cup_Position_v{director.version:D}_draft{director.draft:D}.png";
            var cupPositionImageFilePath = Path.Combine(reportingOutputFolder, cupPositionImageFileName);
            CupExporter.ExportCupPositionImage(director, cupPositionImageFilePath, true);

            // STLs to recreate the image in external tool
            CupExporter.ExportCupPositionParts(reportingOutputFolder, director);

            // Export Transition
            var color = Amace.Visualization.Colors.Metal;
            StlUtilities.RhinoMesh2StlBinary(fullPlateWithTransition,
                Path.Combine(reportingOutputFolder, $@"{director.Inspector.CaseId}_Plate_with_Transition_v{director.version:D}_draft{director.draft:D}.stl"), 
                new int[] { color.R, color.G, color.B });

            // Zip file
            CompressToZipFileWithSameName(reportingOutputFolder);

            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done exporting for Reporting.");
        }

        /// <summary>
        /// Exports for post processing.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="screwBumpTransition">Transition for Screw Bump</param>
        /// <param name="flangeToCupTransition">Transition for Flange to Cup</param>
        public static void ExportForPostProcessing(ImplantDirector director, Mesh screwBumpTransition, Mesh flangeToCupTransition)
        {
            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Exporting for Finalization...");

            // Get list of building blocks
            var postProcessingExportBuildingBlocks = ExportBuildingBlocks.GetExportBuildingBlockListPostProcessing()
                .Select(x => BuildingBlocks.Blocks[x]).ToList();

            // get post proc output folder
            var finalizationOutputFolder = DirectoryStructure.GetFinalizationOutputFolder(director);

            // For each exported mesh: its mesh, description/name, STL path
            BlockExporter.ExportBuildingBlocks(director, postProcessingExportBuildingBlocks, finalizationOutputFolder);

            // Export Acetabular plane
            CupExporter.ExportAcetabularPlane(finalizationOutputFolder, director);

            // Export Transitions
            var flangeColor = Amace.Visualization.Colors.TransitionPreview;
            var bumpColor = Amace.Visualization.Colors.PorousOrange;
            StlUtilities.RhinoMesh2StlBinary(screwBumpTransition,
                Path.Combine(finalizationOutputFolder, $@"{director.Inspector.CaseId}_Bump_Transition_v{director.version:D}_draft{director.draft:D}.stl"),
                new int[] { bumpColor.R, bumpColor.G, bumpColor.B });
            StlUtilities.RhinoMesh2StlBinary(flangeToCupTransition,
                Path.Combine(finalizationOutputFolder, $@"{director.Inspector.CaseId}_Flange_Transition_v{director.version:D}_draft{director.draft:D}.stl"), 
                new int[] { flangeColor.R, flangeColor.G, flangeColor.B });

            // Zip file
            CompressToZipFileWithSameName(finalizationOutputFolder);

            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done exporting for Finalization.");
        }

        public static void ExportForVirtualBenchTest(ImplantDirector director)
        {
            if (director.AmaceFea == null)
            {
                return;
            }

            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Exporting for Virtual Bench Test...");

            var virtualBenchTestOutputFolder = DirectoryStructure.GetVirtualBenchTestOutputFolder(director);
            var inpFileName = $"{director.Inspector.CaseId}_Virtual_Bench_Test_v{director.version:D}_draft{director.draft:D}.inp";
            var inpFilePath = Path.Combine(virtualBenchTestOutputFolder, inpFileName);
            director.AmaceFea.inp.InpFile = inpFilePath;
            director.AmaceFea.inp.Write();

            // Zip file
            CompressToZipFileWithSameName(virtualBenchTestOutputFolder);

            // Status
            IDSPluginHelper.WriteLine(LogCategory.Default, "Done exporting for Virtual Bench Test.");
        }

        private static void CompressToZipFileWithSameName(string folder)
        {
            var zipFile = $"{folder}.zip";
            ZipFile.CreateFromDirectory(folder, zipFile, CompressionLevel.Optimal, false);

            SystemTools.DeleteRecursively(folder);
        }
    }
}
