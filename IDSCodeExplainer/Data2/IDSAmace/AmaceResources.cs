using System.IO;

namespace IDS.Amace
{
    public class AmaceResources : Core.PluginHelper.Resources, Core.Quality.IQCResources
    {
        public string CalculixExecutable => Path.Combine(CalculixFolder, "ccx29.exe");

        public string CalculixFolder => Path.Combine(ExternalToolsFolder, "Calculix64");

        /// <summary>
        /// Get the screw database file path.
        /// </summary>
        public string CupPanelIconFile => Path.Combine(AssetsFolder, "CupPanelIcon.ico");
        
        /// <summary>
        /// Gets the qc document CSS file.
        /// </summary>
        /// <value>
        /// The qc document CSS file.
        /// </value>
        public string qcDocumentCssFile => Path.Combine(AssetsFolder, "style.css");
        public string qcDocumentCssTestVersionFile => Path.Combine(AssetsFolder, "style.css"); //TODO: Have test version
        /// <summary>
        /// Gets the qc document HTML file.
        /// </summary>
        /// <value>
        /// The qc document HTML file.
        /// </value>
        public string qcDocumentHtmlFile => Path.Combine(AssetsFolder, "IDS_QC_Report_Template.html");

        /// <summary>
        /// Gets the qc document java script file.
        /// </summary>
        /// <value>
        /// The qc document java script file.
        /// </value>
        public string qcDocumentJavaScriptFile => Path.Combine(AssetsFolder, "scripting.js");

        /// <summary>
        /// Get the screw database file path.
        /// </summary>
        public string ScrewDatabasePath => Path.Combine(AssetsFolder, "IDS_Screw_Database_Full.3dm");

        /// <summary>
        /// Gets the screw panel icon file.
        /// </summary>
        /// <value>
        /// The screw panel icon file.
        /// </value>
        public string ScrewPanelIconFile => Path.Combine(AssetsFolder, "ScrewPanelIcon.ico");

        /// <summary>
        /// Gets the python modules folder.
        /// </summary>
        /// <value>
        /// The python modules folder.
        /// </value>
        public string TetgenExecutable => Path.Combine(ExternalToolsFolder, "tetgen.exe");
        
        public string GuideFatCupWithFenestrationsStepFile => Path.Combine(AssetsFolder, "Guide_Fat_Cup_With_Fenestrations.stp");
        public string GuideLiftTabStl => Path.Combine(AssetsFolder, "Guide_Lift_Tab.stl");
        public string GuideSnapFitStl => Path.Combine(AssetsFolder, "Guide_Snap_Fit.stl");
        public string GuideSnapFitSubtractorStepFile => Path.Combine(AssetsFolder, "Guide_Snap_Fit_Subtractor.stp");
        public string ScrewDatabaseXmlPath => Path.Combine(AssetsFolder, "Screw_Database.xml");
        public string ScrewDatabaseXmlSchemaPath => Path.Combine(AssetsFolder, "Screw_Database.xsd");
        public string PreferencesPath => Path.Combine(ExecutingPath, "Preferences");
        public string AMacePreferenceXmlPath => Path.Combine(PreferencesPath, "IDSAMacePreferences.xml");
    }
}