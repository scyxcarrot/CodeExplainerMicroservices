using System.IO;
using System.Reflection;

namespace IDS.CMFImplantCreation.UnitTests
{
    public class TestResources
    {
        private readonly string _executingPath =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.ToString();

        public TestResources()
        {
        }

        public string OptimizeOffsetDataPath => Path.Combine(_executingPath, "Resources", "OptimizeOffsetData");

        public string StitchMeshDataPath => Path.Combine(_executingPath, "Resources", "StitchMeshData");

        public string GenerateConnectionWithSharpCurvesDataPath => Path.Combine(_executingPath, "Resources", "GenerateConnectionWithSharpCurvesData");

        public string GenerateConnectionDataPath => Path.Combine(_executingPath, "Resources", "GenerateConnectionData");

        public string GenerateSharpConnectionDataPath => Path.Combine(_executingPath, "Resources", "GenerateSharpConnectionData");

        public string ConnectionDataPath => Path.Combine(_executingPath, "Resources", "ConnectionData");
    }
}
