using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

namespace IDS.Core.PluginHelper
{
    public class Resources
    {
        public string CalculixExecutable => Path.Combine(CalculixFolder, "ccx29.exe");

        public string CalculixFolder => Path.Combine(ExternalToolsFolder, "Calculix64");

        /// <summary>
        /// Gets the python modules folder.
        /// </summary>
        /// <value>
        /// The python modules folder.
        /// </value>
        public string TetgenExecutable => Path.Combine(ExternalToolsFolder, "tetgen.exe");

        private static string IDSWebsiteBaseUrl => "https://home.materialise.net/sites/Materialise%20Software/Implant%20Design%20Suite/General%20Documents/Website/IDS/";

        public string IdsHelpGeneralInfo => IDSWebsiteBaseUrl + "GeneralInfo.html";

        public string IdsHelpToolbars => IDSWebsiteBaseUrl + "Toolbars.html";

        /// <summary>
        /// Gets the toolbar help URL. 
        /// </summary>
        /// <param name="toolbarAnchor">The html id/anchor corresponding to the toolbar.</param>
        /// <returns></returns>
        public string GetToolbarHelpUrl(string toolbarAnchor)
        {
            return $"{IdsHelpToolbars}#{toolbarAnchor}";
        }

        public string AcvdExecutable => Path.Combine(ExternalToolsFolder, "ACVD");

        public string CPythonExecutable
        {
            get
            {
                //Temporary solution, see if preferences is required, 
                //if yes a proper Preferences implementation needs to be done.
                //else can remove this in future
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(ExecutingPath + "\\IDSPreferences.xml");

                var nodePath = xmlDoc.SelectSingleNode("/Paths");

                Debug.Assert(nodePath != null, "nodePath != null");

                return nodePath.SelectSingleNode("IDS_Python")?.InnerText;
            }
        }

        public int IdleConfirmationTimeMs
        {
            get
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(ExecutingPath + "\\IDSPreferences.xml");

                var nodePath = xmlDoc.SelectSingleNode("/Paths");

                Debug.Assert(nodePath != null, "nodePath != null");
                var waitingTimeText = nodePath.SelectSingleNode("IdleConfirmationTimeMs")?.InnerText;
                if (waitingTimeText == null || !int.TryParse(waitingTimeText, out var waitingTime))
                {
                    return 30000;// As default value if missing
                }
                return waitingTime;
            }
        }

        public bool SuppressPromptMessage
        {
            get
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(ExecutingPath + "\\IDSPreferences.xml");

                var nodePath = xmlDoc.SelectSingleNode("/Paths");

                Debug.Assert(nodePath != null, "nodePath != null");

                var suppressPromptStr = nodePath.SelectSingleNode("SuppressPromptMessage")?.InnerText;
                if (suppressPromptStr == null || !bool.TryParse(suppressPromptStr, out var suppressPromptFlag))
                {
                    return false;// As default value if missing
                }
                return suppressPromptFlag;
            }
        }

        /// <summary>
        /// Return the path where the plugin's Python scripts are located
        /// </summary>
        public string CPythonScriptsFolder => Path.Combine(PythonModulesFolder, "CPython");

        /// <summary>
        /// Get the screw database file path.
        /// </summary>
        public string DummyFilePath => Path.Combine(AssetsFolder, "DummyFile.3dm");

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

        /// <summary>
        /// Gets the ids settings file.
        /// </summary>
        /// <value>
        /// The ids settings file.
        /// </value>
        public string IdsSettingsFile => Path.Combine(AssetsFolder, "IDSsettings.ini");

        /// <summary>
        /// Gets the ids versionsfile.
        /// Contains the Git hashes of all components for every IDS version.
        /// </summary>
        /// <value>
        /// The ids versionsfile.
        /// </value>
        public string IdsVersionsfile => Path.Combine(ExecutingPath, "IDSversions.xml");

        /// <summary>
        /// Return the path where the plugin's Python scripts are located
        /// </summary>
        public string IronPythonScriptsFolder => Path.Combine(PythonModulesFolder, "IronPython");

        /// <summary>
        /// Gets the versionsfile.
        /// Contains the Git hashes of the currently used IDS version.
        /// </summary>
        /// <value>
        /// The versionsfile.
        /// </value>
        public string Versionsfile => Path.Combine(ExecutingPath, "Versions.xml");

        /// <summary>
        /// Gets the assets folder.
        /// </summary>
        /// <value>
        /// The assets folder.
        /// </value>
        public string AssetsFolder => Path.Combine(ExecutingPath, "Assets");

        /// <summary>
        /// Gets the python modules folder.
        /// </summary>
        /// <value>
        /// The python modules folder.
        /// </value>
        private string PythonModulesFolder => Path.Combine(ExecutingPath, "PythonModules");

        /// <summary>
        /// Returns the path to a script in the CPython scripts folder
        /// </summary>
        /// <param name="scriptname"></param>
        /// <returns></returns>
        public string GetCPythonScriptPath(string scriptname)
        {
            return Path.Combine(CPythonScriptsFolder, $"{scriptname}.py");
        }

        public string IronPythonSitePackagesFolder => Path.Combine(IronPythonScriptsFolder, "site-packages");
        public string IronPythonDllsFolder => Path.Combine(IronPythonScriptsFolder, "DLLs");
        public string PyGeneralFunctionsFolder => Path.Combine(PythonModulesFolder, "PyGeneralFunctions");

        //Injects python dependencies to local python environment
        public string GeneratePythonOsEnvironmentSetUpScript()
        {
            return "import os,sys\r\n" +
                   $"sys.path.append(r\'{IronPythonSitePackagesFolder}\')\r\n" +
                   $"sys.path.append(r\'{IronPythonDllsFolder}\')\r\n" +
                   $"sys.path.append(r\'{IronPythonScriptsFolder}\')\r\n" +
                   $"sys.path.append(r\'{CPythonScriptsFolder}\')\r\n" +
                   $"sys.path.append(r\'{PyGeneralFunctionsFolder}\')\r\n";
        }
    }
}