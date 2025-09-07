using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class ExpectedOptimizeOffsetOutput : Importer
    {
        private const string OutputDirName = "Outputs";
        private const string VertexOffsettedLowerFileName = "VertexOffsettedLower.json";
        private const string VertexOffsettedUpperFileName = "VertexOffsettedUpper.json";
        private const string TopMeshFileName = "Top.stl";
        private const string BottomMeshFileName = "Bottom.stl";

        public IList<IPoint3D> VertexOffsettedLower { get; }
        public IList<IPoint3D> VertexOffsettedUpper { get; }
        public IMesh TopMesh { get; }
        public IMesh BottomMesh { get; }

        public ExpectedOptimizeOffsetOutput(string workingDir)
        {
            var dir = Path.Combine(workingDir, OutputDirName);

            var vertexOffsettedLowerPath = Path.Combine(dir, VertexOffsettedLowerFileName);
            VertexOffsettedLower = ImportJson<List<IDSPoint3D>>(vertexOffsettedLowerPath)
                .Cast<IPoint3D>().ToList();

            var vertexOffsettedUpperPath = Path.Combine(dir, VertexOffsettedUpperFileName);
            VertexOffsettedUpper = ImportJson<List<IDSPoint3D>>(vertexOffsettedUpperPath)
                .Cast<IPoint3D>().ToList();

            var console = new TestConsole();
            var topMeshPath = Path.Combine(dir, TopMeshFileName);
            TopMesh = ImportMeshWithoutIdenticalVertices(topMeshPath, console); 

            var bottomMeshPath = Path.Combine(dir, BottomMeshFileName);
            BottomMesh = ImportMeshWithoutIdenticalVertices(bottomMeshPath, console);
        }
    }
}
