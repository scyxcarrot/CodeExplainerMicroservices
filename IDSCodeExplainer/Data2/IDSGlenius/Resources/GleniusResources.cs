using IDS.Core.Quality;
using System.IO;

namespace IDS.Glenius
{
    //TODO: TechDebt, should put in Glenius Project Folder Asset
    public class Resources : Core.PluginHelper.Resources, IQCResources
    {
        public ScrewResources Screws { get; } = new ScrewResources();

        public string GleniusColorsXmlFile
        {
            get
            {
                return Path.Combine(AssetsFolder, "Glenius_Colors.xml");
            }
        }

        public string GleniusReconstructionFolder
        {
            get
            {
                return Path.Combine(ExternalToolsFolder, "Reconstruction");
            }
        }

        public string GleniusReconstructionSSMExecutablePath
        {
            get
            {
                return Path.Combine(GleniusReconstructionFolder, "GleniusReconstruction.exe");
            }
        }

        public string GleniusReconstructionSSMDataPath
        {
            get
            {
                return Path.Combine(GleniusReconstructionFolder, "ssmfile.mat");
            }
        }

        public string Head36StepFile
        {
            get
            {
                return Path.Combine(AssetsFolder, "36_Head.stp");
            }
        }

        public string Head38StepFile
        {
            get
            {
                return Path.Combine(AssetsFolder, "38_Head.stp");
            }
        }

        public string Head42StepFile
        {
            get
            {
                return Path.Combine(AssetsFolder, "42_Head.stp");
            }
        }

        public string CommonCylinderStepFile
        {
            get
            {
                return Path.Combine(AssetsFolder, "Common_Cylinder.stp");
            }
        }

        public string CommonProductionRodStepFile
        {
            get
            {
                return Path.Combine(AssetsFolder, "Common_ProductionRod.stp");
            }
        }

        public string CommonCylinderWithTransitionStepFile
        {
            get
            {
                return Path.Combine(AssetsFolder, "Common_Cylinder_Transition.stp");
            }
        }

        public string CommonProductionRodChamferStepFile
        {
            get
            {
                return Path.Combine(AssetsFolder, "Common_ProductionRodChamfer.stp");
            }
        }

        public string CommonTaperAndSafetyStepFile
        {
            get
            {
                return Path.Combine(AssetsFolder, "Common_TaperAndSafety.stp");
            }
        }

        public string HeadPanelIcon => Path.Combine(AssetsFolder, "HeadPanelIcon.ico");

        public string TaperMantle => Path.Combine(AssetsFolder, "Common_TaperMantle.stp");

        public string TaperBooleanReal => Path.Combine(AssetsFolder, "Common_TaperBooleanReal.stp");

        public string TaperBooleanProduction => Path.Combine(AssetsFolder, "Common_TaperBooleanProduction.stp");

        public string CylinderOffset => Path.Combine(AssetsFolder, "Common_CylinderOffset.stp");

        public string ReferenceBlock => Path.Combine(AssetsFolder, "Common_ReferenceBlock.stp");

        public string qcDocumentCssFile => Path.Combine(AssetsFolder, "QCDoc", "style.css");
        public string qcDocumentCssTestVersionFile => Path.Combine(AssetsFolder, "style.css"); //TODO: Have test version

        public string qcDocumentHtmlFile => Path.Combine(AssetsFolder, "QCDoc", "IDS_QC_Report_Template.html");

        public string qcDocumentJavaScriptFile => Path.Combine(AssetsFolder, "QCDoc", "scripting.js");

        public string GuideHandle => Path.Combine(AssetsFolder, "Guide_Handle.stl");

        public string PlasticHead36 => Path.Combine(AssetsFolder, "36_PlasticHead.stl");

        public string PlasticHead38 => Path.Combine(AssetsFolder, "38_PlasticHead.stl");

        public string PlasticHead42 => Path.Combine(AssetsFolder, "42_PlasticHead.stl");

        public string UnroundedCylinder => Path.Combine(AssetsFolder, "Common_CylinderUnrounded.stp");

        public string PlaneXmlSchemaPath => Path.Combine(AssetsFolder, "Plane.xsd");

        public string SphereXmlSchemaPath => Path.Combine(AssetsFolder, "Sphere.xsd");

        public string GleniusIDSXmlFile => Path.Combine(AssetsFolder, "Glenius_IDS.xml");

        public string GleniusIDSXmlSchemaPath => Path.Combine(AssetsFolder, "Glenius_IDS.xsd");

        public string TaperBooleanPlastic => Path.Combine(AssetsFolder, "Common_TaperBooleanPlastic.stp");

        private static string GleniusIDSWebsiteBaseUrl => "https://home.materialise.net/sites/Materialise%20Software/Implant%20Design%20Suite/General%20Documents/Website/IDS/";

        public string GleniusGeneralInfoUrl => GleniusIDSWebsiteBaseUrl + "GeneralInfoGlenius.html";

        private static string GleniusToolbarUrl => GleniusIDSWebsiteBaseUrl + "ToolbarsGlenius.html";

        public string GleniusToolbarProjectUrl => GleniusToolbarUrl + "#project";

        public string GleniusToolbarReconstructionUrl => GleniusToolbarUrl + "#reconstruction";

        public string GleniusToolbarHeadUrl => GleniusToolbarUrl + "#head";

        public string GleniusToolbarScrewUrl => GleniusToolbarUrl + "#screws";

        public string GleniusToolbarScrewQcUrl => GleniusToolbarUrl + "#screwqc";

        public string GleniusToolbarPlateUrl => GleniusToolbarUrl + "#plate";

        public string GleniusToolbarScaffoldUrl => GleniusToolbarUrl + "#scaffold";

        public string GleniusToolbarScaffoldQcUrl => GleniusToolbarUrl + "#scaffoldqc";
    }
}