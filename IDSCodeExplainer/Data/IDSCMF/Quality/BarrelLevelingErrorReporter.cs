using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.CMF.Quality
{
    public static class BarrelLevelingErrorReporter
    {

        public static void ReportGuideBarrelLevelingError(Mesh guideSupport, List<Screw> implantScrews)
        {
            if (guideSupport == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning,
                    $"Leveling are not carried out for {implantScrews.Count} Registered Barrel(s)" +
                    $" because guide support mesh is not available. Please import the guide support mesh to apply leveling.");
            }
            else if (guideSupport.Vertices.Count < 1000)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning,
                    $"Leveling are not carried out for {implantScrews.Count} Registered Barrel(s)" +
                    $" because guide support mesh is unusually low in triangle count. Please check your guide support and import again.");
            }
            else if (!guideSupport.IsValid)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning,
                    $"Leveling are not carried out for {implantScrews.Count} Registered Barrel(s)" +
                    $" because guide support mesh is invalid. Please fix and import guide support again.");
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"Leveling are not carried out for {implantScrews.Count} Registered Barrel(s)" +
                    $" for unknown reason. Kindly check your guide support if fixing is needed and import it again, " +
                    $"also file a bug with the problem Id reported so development team can look into this.");
            }
        }
    }
}
