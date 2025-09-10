using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using System.IO;

namespace IDS.CMFImplantCreation.UnitTests
{
    internal class ConnectionInput : Importer
    {
        private const string InputDirectoryName = "Inputs";
        private const string ConnectionCurveFileName = "connectionCurve.json";
        private const string SupportMeshFullFileName = "supportMeshFull.stl";
        private const string SupportMeshRoIFileName = "supportMeshRoI.stl";
        private const string ConnectionComponentInputValueFileName = "connectionComponentInputValue.json";

        public ICurve ConnectionCurve { get; }
        public IMesh SupportMeshFull { get; }
        public IMesh SupportMeshRoI { get; }
        public ConnectionInputValue ConnectionInputValue { get; }

        public ConnectionInput(string workingDirectory)
        {
            var inputDirectoryPath = Path.Combine(workingDirectory, InputDirectoryName);
            var console = new TestConsole();

            var connectionCurvePath = Path.Combine(inputDirectoryPath, ConnectionCurveFileName);
            var jsonConnectionCurve = ImportJson<IDSCurveForJson>(connectionCurvePath);
            ConnectionCurve = jsonConnectionCurve.GetICurve();

            var supportMeshFullPath = Path.Combine(inputDirectoryPath, SupportMeshFullFileName);
            SupportMeshFull = ImportMeshWithoutIdenticalVertices(supportMeshFullPath, console);

            var supportMeshRoIPath = Path.Combine(inputDirectoryPath, SupportMeshRoIFileName);
            SupportMeshRoI = ImportMeshWithoutIdenticalVertices(supportMeshRoIPath, console);

            var connectionComponentInputValuePath = Path.Combine(
                inputDirectoryPath, ConnectionComponentInputValueFileName);
            ConnectionInputValue = ImportJson<ConnectionInputValue>(
                connectionComponentInputValuePath);
        }
    }
}