using IDS.Interface.Geometry;
using System.IO;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class ExpectedConnectionOutput : Importer
    {
        private const string OutputDirectoryName = "Outputs";
        private const string ConnectionMeshFileName = "connection.stl";

        public IMesh ConnectionMesh { get; }

        public ExpectedConnectionOutput(string workingDirectory)
        {
            var outputDirectoryPath = Path.Combine(workingDirectory, OutputDirectoryName);
            var console = new TestConsole();

            var connectionMeshPath = Path.Combine(outputDirectoryPath, ConnectionMeshFileName);
            ConnectionMesh = ImportMeshWithoutIdenticalVertices(connectionMeshPath, console);
        }
    }
}