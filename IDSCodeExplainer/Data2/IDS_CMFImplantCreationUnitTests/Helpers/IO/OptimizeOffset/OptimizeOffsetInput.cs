using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System.Collections.Generic;
using System.IO;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class OptimizeOffsetInput : Importer
    {
        public class IDSDeserializableMesh
        {
            public List<IDSVertex> Vertices { get; set; } = new List<IDSVertex>();
            public List<IDSFace> Faces { get; set; } = new List<IDSFace>();

            public IMesh ToIMesh()
            {
                return new IDSMesh(Vertices, Faces);
            }
        }

        private const string InputDirName = "Inputs";
        private const string OptimizeOffsetInputValueFileName = "OptimizeOffsetInputValue.json";
        private const string ConnectionSurfaceFileName = "ConnectionSurface.json";
        private const string SupportFileName = "Support.stl";

        public OptimizeOffsetInputValue OptimizeOffset { get; }
        public IMesh ConnectionSurface { get; }
        public IMesh Support { get; }

        public OptimizeOffsetInput(string workingDir)
        {
            var dir = Path.Combine(workingDir, InputDirName);

            var optimizeOffsetPath = Path.Combine(dir, OptimizeOffsetInputValueFileName);
            OptimizeOffset = ImportJson<OptimizeOffsetInputValue>(optimizeOffsetPath);

            var connSurfacePath = Path.Combine(dir, ConnectionSurfaceFileName);
            ConnectionSurface = ImportJson<IDSDeserializableMesh>(connSurfacePath).ToIMesh();

            var supportPath = Path.Combine(dir, SupportFileName);
            Support = ImportMeshWithoutIdenticalVertices(supportPath, new TestConsole());
        }
    }
}
