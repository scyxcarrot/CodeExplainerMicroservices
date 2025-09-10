using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class ExpectedGenerateConenctionOutput : Importer
    {
        private const string OutputDirectoryName = "Outputs";
        private const string ConnectionMeshFileName = "implantTube.stl";
        private const string SharpCurvesFileName = "sharpCurves.json";

        public IMesh ConnectionMesh { get; }
        public List<ICurve> SharpCurves { get; }

        public ExpectedGenerateConenctionOutput(string workingDirectory)
        {
            var outputDirectoryPath = Path.Combine(workingDirectory, OutputDirectoryName);
            var console = new TestConsole();

            var connectionMeshPath = Path.Combine(outputDirectoryPath, ConnectionMeshFileName);
            ConnectionMesh = ImportMeshWithoutIdenticalVertices(connectionMeshPath, console); 

            var sharpCurvesPath = Path.Combine(outputDirectoryPath, SharpCurvesFileName);
            var jsonCurves = ImportJson<List<IDSCurveForJson>>(sharpCurvesPath);
            SharpCurves = jsonCurves
                .Select(jsonCurve => jsonCurve.GetICurve())
                .ToList();
        }
    }
}
