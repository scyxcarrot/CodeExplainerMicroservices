using IDS.Common.SystemUtilities;
using IDS.Operations.Reaming;
using IDS.Operations.CupPositioning;
using IDS.Operations.Screws;
using IDS.FileSystem;
using IDS.Common.Enumerators;
using IDS.Operations;
using IDS.Common.Stl;
using IDS.ImplantBuildingBlocks;
using IDS.Visualization;
using System;
using System.Collections.Generic;
using System.IO;
using IDS.Common;

namespace IDS.Operations.Export
{
    // DirectoryExporter implements functionality for exporting stuff out of the rhino project for
    // reporting or further postprocessing
    internal class DirectoryExporter
    {
        // Export3dmProject will save the 3dm file for export and tag it as a new version
        public static bool Export3dmProject(ImplantDirector director)
        {
            // Set the document type as an export document
            DocumentType prevDocType = director.documentType;
            director.documentType = DocumentType.Export;

            // Get directory to save it in
            string outputDir = DirectoryStructure.GetOutputFolderPath(director);

            // Save projects as
            Rhino.FileIO.FileWriteOptions options = new Rhino.FileIO.FileWriteOptions();
            string draftProjectFile = String.Format("{0}\\{1}_export_v{3:D}_draft{2:D}.3dm", outputDir, director.inspector.caseId, director.draft, director.version);
            // Read-write access so it can be overwritten
            if (File.Exists(draftProjectFile))
                System.IO.File.SetAttributes(draftProjectFile, System.IO.FileAttributes.Normal);
            director.document.WriteFile(draftProjectFile, options);

            // Set file to read only
            System.IO.File.SetAttributes(draftProjectFile, System.IO.FileAttributes.ReadOnly);

            // Reset project doctype of current file
            director.documentType = prevDocType;

            // Success
            return true;
        }

        // ExportForPlasticProduction will export everything necessary for plastic production
        public static bool ExportForPlasticProduction(ImplantDirector director, bool openFolder = false)
        {
            // Export directory
            string workingDir = DirectoryStructure.GetWorkingDir(director);

            // get plastic production output folder
            string outputDir = DirectoryStructure.GetPlasticProdOutputFolder(director, cleanFirst: true);

            // Make exportable blocks depend on design phase
            List<IBB> exportedBlocks = new List<IBB> { IBB.ScrewPlasticSubtractor };

            // For each exported mesh: its mesh, description/name, STL path
            bool success = BlockExporter.ExportBuildingBlocks(director, exportedBlocks, outputDir);
            if (!success)
                return false;

            // Export Acetabular plane
            success = CupExporter.ExportAcetabularPlane(outputDir, director);
            if (!success)
                return false;

            // Open the folder via a shell script
            if (openFolder)
            {
                success = SystemTools.OpenExplorerInFolder(outputDir);
                if (!success)
                    return false;
            }

            // Success
            return true;
        }

        // ExportForGuide will export everything necessary for guide design
        public static bool ExportForGuide(ImplantDirector director, bool openFolder = false)
        {
            // Init
            bool success;

            // Export directory
            string workingDir = DirectoryStructure.GetWorkingDir(director);

            // get guide output folder
            string outputDir = DirectoryStructure.GetGuideOutputFolder(director, cleanFirst: true);

            // Horizontal border
            string exportFilePath = string.Format("{0}\\{1}_HorizontalBorder_v{3:D}_Draft{2:D}.stl", outputDir, director.inspector.caseId, director.draft, director.version);
            int[] meshColor = Colors.GetColorArray(Colors.horBorder);
            StlUtilities.RhinoMesh2StlBinary(director.cup.horizontalBorderMesh, exportFilePath, meshColor);

            // Screw csv for guide
            string csvPath;
            success = ScrewExporter.ExportGuideToolCSV(director, outputDir, out csvPath);
            if (!success)
                return false;

            // Open the folder via a shell script
            if (openFolder)
            {
                success = SystemTools.OpenExplorerInFolder(outputDir);
                if (!success)
                    return false;
            }

            //Success
            return true;
        }

        // ExportForReporting will export everything necessary for booklet/imed
        public static bool ExportForReporting(ImplantDirector director, bool openFolder = false)
        {
            // Make exportable blocks depend on design phase
            List<IBB> exportedBlocks = new List<IBB>{   IBB.CupStuds,
                                                        IBB.TotalRbv,
                                                        IBB.OriginalReamedPelvis,
                                                        IBB.Screw,
                                                        IBB.ScaffoldFinalized,
                                                        IBB.PlateSmoothHoles,
                                                        IBB.CollisionEntity };

            // Export directory
            string workingDir = DirectoryStructure.GetWorkingDir(director);

            // get post proc output folder
            string outputDir = DirectoryStructure.GetReportingOutputFolder(director, cleanFirst: true);

            // For each exported mesh: its mesh, description/name, STL path
            bool success = BlockExporter.ExportBuildingBlocks(director, exportedBlocks, outputDir);
            if (!success)
                return false;

            // Cup csv file
            string prefix = string.Format("{0}_v{2:D}_draft{1:D}", director.inspector.caseId, director.draft, director.version);
            success = CupExporter.ExportBookletCSV(outputDir, prefix, director.cup);
            if (!success)
                return false;

            // Screw csv file
            success = ScrewExporter.ExportBookletCSV(director, outputDir, prefix);
            if (!success)
                return false;

            // Reaming csv file
            success = ReamingExporter.ExportBookletCSV(director, outputDir, prefix);
            if (!success)
                return false;

            // Parameter file
            ParameterExporter.ExportParameterFile(director, string.Format("{0}\\{1}_Design_Parameters_v{2:D}_draft{3:D}.txt", outputDir, director.inspector.caseId, director.version, director.draft));

            // Cup position data Image
            string filenameWithOverlay = string.Format(@"{0}\{1}_Cup_Position_v{2:D}_draft{3:D}.png", outputDir, director.inspector.caseId, director.version, director.draft);
            CupExporter.ExportCupPositionImage(director, filenameWithOverlay, true);
            // STLs to recreate the image in external tool
            CupExporter.ExportCupPositionParts(outputDir, director);

            // Screw number overlay
            ScrewExporter.ExportScrewNumberImage(director, string.Format("{0}\\{1}_Screw_Numbers_Overlay_v{2:D}_draft{3:D}.png", outputDir, director.inspector.caseId, director.version, director.draft), true, false);

            // Open the folder via a shell script
            if (openFolder)
            {
                success = SystemTools.OpenExplorerInFolder(outputDir);
                if (!success)
                    return false;
            }

            // Success
            return true;
        }

        // ExportForPostProc will export everything necessary for implant post-processing
        public static bool ExportForPostProc(ImplantDirector director, bool openFolder = false)
        {
            // Define which blocks need to be exported
            List<IBB> exportedBlocks = new List<IBB>{IBB.DefectPelvis, IBB.CupStuds, IBB.CupPorousLayer,
                                                        IBB.ReamedPelvis, IBB.DesignPelvis, IBB.OriginalReamedPelvis,
                                                        IBB.Cup,
                                                        IBB.TotalRbv, IBB.CupReamingEntity, IBB.ExtraReamingEntity,
                                                        IBB.CollisionEntity,
                                                        IBB.ScaffoldVolume, IBB.ScaffoldFinalized, IBB.SkirtMesh,
                                                        IBB.MedialBump,IBB.MedialBumpTrim,IBB.LateralBump,IBB.LateralBumpTrim,
                                                        IBB.Screw, IBB.ScrewContainer, IBB.ScrewHoleSubtractor,
                                                        IBB.LateralCupSubtractor, IBB.ScrewMbvSubtractor,
                                                        IBB.ScrewCushionSubtractor, IBB.ScrewOutlineEntity,
                                                        IBB.PlateSmoothHoles, IBB.SolidPlateBottom,
                                                        IBB.FilledSolidCup,IBB.SolidPlateRounded,
                                                        };

            // Export directory
            string workingDir = DirectoryStructure.GetWorkingDir(director);

            // get post proc output folder
            string outputDir = DirectoryStructure.GetFinalizationOutputFolder(director, cleanFirst: true);

            // For each exported mesh: its mesh, description/name, STL path
            bool success = BlockExporter.ExportBuildingBlocks(director, exportedBlocks, outputDir);
            if (!success)
                return false;

            // Export Acetabular plane
            success = CupExporter.ExportAcetabularPlane(outputDir, director);
            if (!success)
                return false;

            // This can be used to write out an mxp file, currently not used
            //string mxpFile = string.Format("{0}\\{1}_v{3:D}_Draft{2:D}_PostProcessing.mxp", outputDir, director.inspector.CaseID, director.draft, director.version);
            //success = BlockExporter.ExportBuildingBlocksMXP(director, exportedBlocks, mxpFile, draft);

            // Success
            return true;
        }
    }
}