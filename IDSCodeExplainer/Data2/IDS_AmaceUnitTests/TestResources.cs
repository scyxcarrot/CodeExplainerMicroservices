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

        public string InpOutputfile => Path.Combine(UnitTestDataFolder, "simulation_output.inp");

        public string InpInputFile => Path.Combine(UnitTestDataFolder, "simulation.inp");

        public string FrdInputFile => Path.Combine(UnitTestDataFolder, "simulation.frd");

        private string UnitTestDataFolder => Path.Combine(_executingPath, "UnitTestData");

        public string TestScrewDatabaseXmlPath => Path.Combine(UnitTestDataFolder, "Test_Screw_Database.xml");

        public string ScrewDatabaseXmlPath => Path.Combine(UnitTestDataFolder, "Screw_Database.xml");
    }
}