using IDS.Interface.Geometry;
using System.IO;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class ExpectedStitchMeshOutput : Importer
    {
        private const string OutputDirectoryName = "Outputs";
        private const string OffsetMeshFileName = "Offset.stl";
        private const string StitchedMeshFileName = "Stitched.stl";

        public IMesh OffsetMesh { get; }
        public IMesh StitchedMesh { get; }

        public ExpectedStitchMeshOutput(string workingDirectory)
        {
            var outputDirectoryPath = Path.Combine(workingDirectory, OutputDirectoryName);
            var console = new TestConsole();

            var offsetMeshPath = Path.Combine(outputDirectoryPath, OffsetMeshFileName);
            OffsetMesh = ImportMeshWithoutIdenticalVertices(offsetMeshPath, console); 

            var stitchedMeshPath = Path.Combine(outputDirectoryPath, StitchedMeshFileName);
            StitchedMesh = ImportMeshWithoutIdenticalVertices(stitchedMeshPath, console);
        }
    }
}
