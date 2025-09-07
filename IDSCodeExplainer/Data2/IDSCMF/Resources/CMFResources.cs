using IDS.Core.Quality;
using System.IO;

namespace IDS.CMF.FileSystem
{
    public class CMFResources : Core.PluginHelper.Resources, IQCResources
    {
        /// <summary>
        /// Gets the qc document CSS file.
        /// </summary>
        /// <value>
        /// The qc document CSS file.
        /// </value>
        public string qcDocumentCssFile => Path.Combine(AssetsFolder, "cmf_style.css");
        public string qcDocumentCssTestVersionFile => Path.Combine(AssetsFolder, "cmf_style_test_version.css");

        /// <summary>
        /// Gets the qc document HTML file.
        /// </summary>
        /// <value>
        /// The qc document HTML file.
        /// </value>
        public string qcDocumentHtmlFile => Path.Combine(AssetsFolder, "IDS_CMF_QC_Report_Template.html");

        /// <summary>
        /// Gets the qc document java script file.
        /// </summary>
        /// <value>
        /// The qc document java script file.
        /// </value>
        public string qcDocumentJavaScriptFile => Path.Combine(AssetsFolder, "cmf_scripting.js");
        public string qcDocumentImplantDynamicScriptFile => Path.Combine(AssetsFolder, "QC_Implant_Dynamic_Template.html");
        public string qcDocumentImplantBoneThicknessAnalysisDynamicScriptFile => Path.Combine(AssetsFolder, "QC_Implant_Bone_Thickness_Dynamic_Template.html");
        public string qcDocumentGuideDynamicScriptFile => Path.Combine(AssetsFolder, "QC_Guide_Dynamic_Template.html");

        public string qcDocumentImplantScrewQcDynamicScriptFile => Path.Combine(AssetsFolder, "QC_Implant_Screw_Qc_Dynamic_Template.html");

        public string qcDocumentGuideScrewQcDynamicScriptFile => Path.Combine(AssetsFolder, "QC_Guide_Screw_Qc_Dynamic_Template.html");

        public string IdsCmfSettingsFile => Path.Combine(AssetsFolder, "IDSCMFsettings.ini");

        public string IdsCmf2SettingsFile => Path.Combine(AssetsFolder, "IDSCMF2settings.ini");

        public string LoadProplanMatSDKConsole => Path.Combine(ExternalToolsFolder, "CMFProplanConsole\\LoadProPlanConsole.exe");

        public string TrimaticInteropFolder => Path.Combine(ExternalToolsFolder, "TrimaticInterop");

        public string TrimaticQcaToMxpFolder => Path.Combine(TrimaticInteropFolder, "QcaToMxp");

        public string TrimaticQcaStlToMxpPyScript => "QcaToMxp.py";
        public string TrimaticQcaStlToMxpPyScriptPath => Path.Combine(TrimaticQcaToMxpFolder, TrimaticQcaStlToMxpPyScript);

        public string TrimaticImplantSupportSourcesToMxpFolder => Path.Combine(TrimaticInteropFolder, "ImplantSupportSourcesToMxp");

        public string TrimaticImplantSupportSourcesStlToMxpPyScript => "ImplantSupportSourcesToMxp.py";
        public string TrimaticImplantSupportSourcesStlToMxpPyScriptPath => Path.Combine(TrimaticImplantSupportSourcesToMxpFolder, TrimaticImplantSupportSourcesStlToMxpPyScript);

        public string TrimaticGuideSupportSourcesToMxpFolder => Path.Combine(TrimaticInteropFolder, "GuideSupportSourcesToMxp");
        public string TrimaticGuideSupportSourcesStlToMxpPyScript => "GuideSupportSourcesToMxp.py";
        public string TrimaticGuideSupportSourcesStlToMxpPyScriptPath => Path.Combine(TrimaticGuideSupportSourcesToMxpFolder, TrimaticGuideSupportSourcesStlToMxpPyScript);

        public string AutoDeploymentFolder => Path.Combine(ExternalToolsFolder, "AutoDeployment");
        public string AutoDeploymentInstallationProxyScriptPath => Path.Combine(AutoDeploymentFolder, "InstallationProxy.bat");
        public string AutoDeploymentCheckPBAVersionScriptPath => Path.Combine(AutoDeploymentFolder, "CheckPBAVersion.bat");

        public string SmartDesignFolder => Path.Combine(ExternalToolsFolder, "SmartDesign");
        public string SmartDesignExecuteOperationScriptPath => Path.Combine(SmartDesignFolder, "ExecuteSmartDesignOperation.bat");

        public string ProPlanImportNameCompatibleJsonFile => Path.Combine(AssetsFolder, "ProPlanImportNameCompatible.json");

        public string BoneNamePreferencesJsonFile => Path.Combine(AssetsFolder, "BoneNamePreferences.json");

        public string ImplantTemplateXmlPath => Path.Combine(AssetsFolder, "ImplantTemplate.xml");

        public string ImplantTemplateXmlXsdPath => Path.Combine(AssetsFolder, "ImplantTemplate.xsd");

        public string ToggleTransparencyJsonFile => Path.Combine(AssetsFolder, "ToggleTransparency.json");

        public string CasePreferencesFolder => Path.Combine(AssetsFolder, "CasePreferences\\");

        public string ProPlanImportCoordinateSystemFileName => "coordinatesystems.json";

        public string ProPlanImportSagittalPlaneFileName => "sagittalplane.json";

        public string PreferencesPath => Path.Combine(ExecutingPath, "Preferences");
        public string CMFPreferenceXmlPath => Path.Combine(PreferencesPath, "IDSCMFPreferences.xml");
        public string CMFPreferenceXmlXsdPath => Path.Combine(CasePreferencesFolder, "CasePreferencesSchema.xsd");

        public string CasePreferencesScrewsFolder => Path.Combine(CasePreferencesFolder, "Screws");
        public string ScrewPartSpecificationFilePath => Path.Combine(CasePreferencesScrewsFolder, "ScrewPartSpecification.xml");
        public string BarrelPartSpecificationFilePath => Path.Combine(CasePreferencesScrewsFolder, "BarrelPartSpecification.xml");

        public string BoneThicknessAnalysisScaleForARTFileName => "Scale.jpeg";
        public string BoneThicknessAnalysisScaleForARTFilePath => Path.Combine(AssetsFolder, BoneThicknessAnalysisScaleForARTFileName);
    }
}