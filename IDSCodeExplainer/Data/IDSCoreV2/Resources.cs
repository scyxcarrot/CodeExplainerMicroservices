using System.IO;
using System.Reflection;

namespace IDS.Core.V2
{
    public class Resources
    {
        public string MatSdkConsolex86Folder => Path.Combine(ExternalToolsFolder, "MatSDKConsolex86");
        public string MatSdkConsolex86Executable => Path.Combine(MatSdkConsolex86Folder, "MatSDKOperationConsole.exe");

        /// <summary>
        /// Gets the executing path.
        /// </summary>
        /// <value>
        /// The executing path.
        /// </value>
        public string ExecutingPath { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Get the path containing third-party executables.
        /// </summary>
        public string ExternalToolsFolder => Path.Combine(ExecutingPath, "ExternalTools");
    }
}