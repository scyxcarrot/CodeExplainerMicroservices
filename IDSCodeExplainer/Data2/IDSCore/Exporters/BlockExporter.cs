using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Core.Operations
{
    // BlockExporter provides functionality for exporting selections of building blocks as stl files
    // or mxp files
    public static class BlockExporter
    {
        // Export a list of BuildingBlocks to stl files a folder
        public static void ExportBuildingBlocks(IImplantDirector director, List<ImplantBuildingBlock> blocks, string exportDir)
        {
            List<string> exportedFiles;
            var filePrefix = director.caseId;
            var fileSuffix = string.Format("v{1:D}_draft{0:D}", director.draft, director.version);
            ExportBuildingBlocks(director, blocks, exportDir, filePrefix, fileSuffix, out exportedFiles);
        }

        // Export a list of BuildingBlocks to a folder with a custom filenameTag
        public static void ExportBuildingBlocks(IImplantDirector director, List<ImplantBuildingBlock> blocks, string exportDir, string filenameTag)
        {
            List<string> exportedFiles;
            var filePrefix = director.caseId;
            ExportBuildingBlocks(director, blocks, exportDir, filePrefix, filenameTag, out exportedFiles);
        }

        // Export a list of BuildingBlocks to a folder with a custom filePrefix and suffix
        public static void ExportBuildingBlocks(IImplantDirector director, List<ImplantBuildingBlock> blocks, 
            string exportDir, string filePrefix, string fileSuffix, out List<string> exportedFiles)
        {
            // Loop over building blocks
            exportedFiles = new List<string>();
            foreach (var theBlock in blocks)
            {
                // get all info
                Mesh exportMesh;
                string exportFilePath;
                int[] meshColor;
                var success = ImplantBuildingBlockProperties.GetExportReady(theBlock, director, exportDir,
                    filePrefix, fileSuffix, out exportMesh, out exportFilePath, out meshColor);
                if (!success)
                {
                    continue; // do not export, error messages are shown in getExportReady
                }

                StlUtilities.RhinoMesh2StlBinary(exportMesh, exportFilePath, meshColor);
                exportedFiles.Add(exportFilePath);
            }
        }
    }
}