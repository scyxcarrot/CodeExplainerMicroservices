using IDS.Amace.ImplantBuildingBlocks;
using Rhino.Geometry;
using System.Globalization;
using System.IO;

namespace IDS.Amace.Operations
{
    // ReamingExporter provides functionality for exporting a parameter file for the reaming
    internal class ReamingExporter
    {
        // Export a csv file for booklet
        public static bool ExportBookletCsv(ImplantDirector director, string saveFolder, string prefix)
        {
            var objectManager = new AmaceObjectManager(director);

            // Get total RBV
            var totalRbv = objectManager.GetBuildingBlock(IBB.TotalRbv).Geometry as Mesh;

            // Measure reaming volume
            var vm = VolumeMassProperties.Compute(totalRbv);
            var reamingVolCc = vm.Volume / 1000.0; // Convert from mm^3 to cc

            // Write to file
            try
            {
                // make the filepath
                var filePath = $"{saveFolder}\\{prefix}_Reporting_Reaming.csv";

                // Write to file
                File.WriteAllText(filePath, "sep=,\n");
                File.AppendAllText(filePath, "ReamingVolume\n");
                File.AppendAllText(filePath, string.Format(CultureInfo.InvariantCulture, "{0:F1}", reamingVolCc));
            }
            catch
            {
                return false;
            }

            // Success
            return true;
        }
    }
}