using IDS.Interface.Geometry;
using System.IO;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class StitchMeshInput : Importer
    {
        private const string InputDirectoryName = "Inputs";
        private const string TopMeshFileName = "Top.stl";
        private const string BottomMeshFileName = "Bottom.stl";

        public IMesh TopMesh { get; }
        public IMesh BottomMesh { get; }

        public StitchMeshInput(string workingDirectory)
        {
            var inputDirectoryPath = Path.Combine(workingDirectory, InputDirectoryName);
            var console = new TestConsole();

            var topMeshPath = Path.Combine(inputDirectoryPath, TopMeshFileName);
            TopMesh = ImportMeshWithoutIdenticalVertices(topMeshPath, console);

            var bottomMeshPath = Path.Combine(inputDirectoryPath, BottomMeshFileName);
            BottomMesh = ImportMeshWithoutIdenticalVertices(bottomMeshPath, console);
        }
    }
}
