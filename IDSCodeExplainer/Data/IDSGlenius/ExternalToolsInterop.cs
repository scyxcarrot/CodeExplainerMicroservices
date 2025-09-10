using IDS.Core.Utilities;
using System.Globalization;

namespace IDS.Glenius
{
    public class ExternalToolsInterop : ExternalToolInterop
    {
        public static bool GleniusReconstruction(string removedDefectRegionStlPath, string outputResultStlPath, string outputResultParamCsvPath, bool isLeft, int iteration, int nComponents)
        {
            var resources = new Resources();

            // Set up the command
            var cmdArgs = string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\" \"{2}\" \"{3}\" {4} {5} {6}",
                resources.GleniusReconstructionSSMDataPath, removedDefectRegionStlPath, outputResultStlPath, outputResultParamCsvPath, isLeft ? "L" : "R", iteration, nComponents);

            // Execute
            return RunExternalTool(resources.GleniusReconstructionSSMExecutablePath, cmdArgs, string.Empty, false);
        }

    }
}
