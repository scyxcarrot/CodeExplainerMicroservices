using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System.IO;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class GenerateSharpConnectionInput : Importer
    {
        private const string InputDirectoryName = "Inputs";
        private const string IntersectionCurveFileName = "intersectionPolyline.json";
        private const string PulledCurveFileName = "sharpCurve.json";
        private const string SupportMeshFileName = "supportMesh.stl";
        private const string GenerateConnectionInputValueFileName = "generateSharpImplantComponentInputValue.json";

        public ICurve IntersectionCurve { get; }
        public ICurve PulledCurve { get; }
        public IMesh SupportMesh { get; }
        public GenerateSharpConnectionInputValue GenerateConnection { get; }

        public GenerateSharpConnectionInput(string workingDirectory)
        {
            var inputDirectoryPath = Path.Combine(workingDirectory, InputDirectoryName);
            var console = new TestConsole();

            var intersectionCurvePath = Path.Combine(inputDirectoryPath, IntersectionCurveFileName);
            var jsonIntersectionCurve = ImportJson<IDSCurveForJson>(intersectionCurvePath);
            IntersectionCurve = jsonIntersectionCurve.GetICurve();

            var pulledCurvePath = Path.Combine(inputDirectoryPath, PulledCurveFileName);
            var jsonPulledCurve = ImportJson<IDSCurveForJson>(pulledCurvePath);
            PulledCurve = jsonPulledCurve.GetICurve();

            var supportMeshPath = Path.Combine(inputDirectoryPath, SupportMeshFileName);
            SupportMesh = ImportMeshWithoutIdenticalVertices(supportMeshPath, console);

            var generateConnectionInputValue = Path.Combine(inputDirectoryPath, GenerateConnectionInputValueFileName);
            GenerateConnection = ImportJson<GenerateSharpConnectionInputValue>(generateConnectionInputValue);
        }
    }
}
