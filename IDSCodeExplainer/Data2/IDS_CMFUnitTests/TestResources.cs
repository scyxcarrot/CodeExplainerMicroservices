using System.IO;
using System.Reflection;

namespace IDS.Testing
{
    public class TestResources
    {
        private readonly string _executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.ToString();

        public TestResources()
        {
        }

        private string UnitTestDataFolder => Path.Combine(_executingPath, "UnitTestData");

        public string ScrewDatabaseXmlPath => Path.Combine(UnitTestDataFolder, "Screw_Database.xml");

        public string CasePreferencesXmlPath => Path.Combine(_executingPath, "Resources", "Case_Preferences_Valid.xml");
        public string CasePreferencesInvalidXmlPath => Path.Combine(_executingPath, "Resources", "Case_Preferences_Invalid.xml");

        public string BoneNamePreferencesTestPurposePath => Path.Combine(_executingPath, "Resources", "BoneNamePreferencesTestPurpose.json");
        public string ProPlanImportReferencesPath => Path.Combine(_executingPath, "Resources", "ProPlanImportReferences.json");
        public string ProPlanNameCompatibleValidCasePath => Path.Combine(_executingPath, "Resources", "ProPlanNameCompatible_ValidCase.json");

        public string ImplantTemplateInvalidXmlPath => Path.Combine(_executingPath, "Resources", "ImplantTemplate_Invalid.xml");

        public string CompleteWorkflowEnlightCmfFilePath => Path.Combine(_executingPath, "Resources", "OK-Case.mcs");

        public string NoPlannedPartEnlightCmfFilePath => Path.Combine(_executingPath, "Resources", "No-Planned-Part-Case.mcs");

        public string OriginalNerveStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "00MAN_nerve_R.stl");

        public string TrimmedAndRepositionedNerveStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "00MAN_nerve_R_Trimmed_Repositioned.stl");

        public string OriginalRamusStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "01RAM_L.stl");

        public string RepositionedRamusStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "01RAM_L_Repositioned_Collide.stl");

        public string TrimmedRamusStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "01RAM_L_Trimmed.stl");

        public string OriginalMaxillaStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "02MAX.stl");

        public string RepositionedMaxillaStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "02MAX_Repositioned_NoCollision.stl");

        public string OriginalMandibleStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "03MAN_body.stl");

        public string RemeshedMandibleStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "03MAN_body_Remeshed.stl");

        public string RepositionedMandibleStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "03MAN_body_Remeshed_Repositioned.stl");

        public string OriginalGenioStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "05GEN.stl");

        public string FilledHoleGenioStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "05GEN_FillHole.stl");

        public string FilledHoleAndRepositionedGenioStlFilePath => Path.Combine(_executingPath, "Resources", "MeshRepositionedCheck", "05GEN_FillHole_Repositioned.stl");

        public string BoneStlFilePath => Path.Combine(_executingPath, "Resources", "OsteotomyDistanceScrewQcCheck", "Bone.stl");

        public string OsteotomyStlFilePath => Path.Combine(_executingPath, "Resources", "OsteotomyDistanceScrewQcCheck", "Osteotomy.stl");

        public string OsteotomySlantedStlFilePath => Path.Combine(_executingPath, "Resources", "OsteotomyDistanceScrewQcCheck", "OsteotomySlanted.stl");

        public string GuideSolidPatchStlFilePath => Path.Combine(_executingPath, "Resources", "GuideSurfaceUtilitiesCheck", "GuideSolidPatch.stl");

        public string GuideBaseStlFilePath => Path.Combine(_executingPath, "Resources", "GuideSurfaceUtilitiesCheck", "GuideBase.stl");

        public string GuideSurfaceWrapStlFilePath => Path.Combine(_executingPath, "Resources", "GuideSurfaceUtilitiesCheck", "GuideSurfaceWrap.stl");

        public string SPPCFilePath => Path.Combine(_executingPath, "Resources", "Case1.sppc");

        #region Test Data
        public string JsonConfigPath => Path.Combine(_executingPath, "Resources", "JsonConfig");

        public string ImplantScrewSerializationTestDataFilePath => Path.Combine(JsonConfigPath, "Screw", "ImplantScrewSerializationTestData.json");
        #endregion
    }
}