using System.IO;
using System.Reflection;

namespace IDSEnlightCMFIntegration.Testing
{
    public class TestResources
    {
        private readonly string _executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.ToString();

        public TestResources()
        {
        }

        private string TestDataFolder => Path.Combine(_executingPath, "TestData");

        public string EnlightCmfFilePath => Path.Combine(TestDataFolder, "ME20-BEM-QUL.mcs");

        public string GoldenPartStlPath => Path.Combine(TestDataFolder, "01GEN.stl");

        public string GoldenOsteotomyStlPath => Path.Combine(TestDataFolder, "LeFortI.stl");

        public string GoldenSplineStlPath => Path.Combine(TestDataFolder, "03Left nerve.stl");

        public string EnlightCmfFullWorkflowFilePath => Path.Combine(TestDataFolder, "CMF_full_workflow.mcs");

        public string EnlightCmfFullWorkflowWithSingleSplitFilePath => Path.Combine(TestDataFolder, "CMF_full_workflow_singlesplitlefort.mcs");

        public string EnlightCmfInternalNameMappingFilePath => Path.Combine(TestDataFolder, "EnlightCMFInternalNameMapping.json");

        public string EnlightCmfFullWorkflow1FilePath => Path.Combine(TestDataFolder, "CMF_full_workflow_Splint Design_start.mcs");
    }
}