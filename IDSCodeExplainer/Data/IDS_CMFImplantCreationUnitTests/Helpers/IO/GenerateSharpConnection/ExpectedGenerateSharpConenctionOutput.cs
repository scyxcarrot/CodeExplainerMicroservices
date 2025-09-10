using IDS.Interface.Geometry;
using System.IO;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class ExpectedGenerateSharpConenctionOutput : Importer
    {
        private const string OutputDirectoryName = "Outputs";
        private const string ConnectionMeshFileName = "dupimplantTube.stl";

        public IMesh ConnectionMesh { get; }

        public ExpectedGenerateSharpConenctionOutput(string workingDirectory)
        {
            var outputDirectoryPath = Path.Combine(workingDirectory, OutputDirectoryName);
            var console = new TestConsole();

            var connectionMeshPath = Path.Combine(outputDirectoryPath, ConnectionMeshFileName);
            ConnectionMesh = ImportMeshWithoutIdenticalVertices(connectionMeshPath, console); 
        }
    }
}
