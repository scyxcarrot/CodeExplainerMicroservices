using System.IO;

namespace IDS.CMF.V2.FileSystem
{
    public class CMFResourcesV2 : Core.V2.Resources
    {
        /// <summary>
        /// Gets the assets folder.
        /// </summary>
        /// <value>
        /// The assets folder.
        /// </value>
        public string AssetsFolder => Path.Combine(ExecutingPath, "Assets");

        public string ProPlanImportJsonFile => Path.Combine(AssetsFolder, "ProPlanImport.json");

        public string ProPlanImportCoordinateSystemFileName => "coordinatesystems.json";

        public string LoadProplanMatSDKConsole => Path.Combine(ExternalToolsFolder, "CMFProplanConsole\\LoadProPlanConsole.exe");
    }
}