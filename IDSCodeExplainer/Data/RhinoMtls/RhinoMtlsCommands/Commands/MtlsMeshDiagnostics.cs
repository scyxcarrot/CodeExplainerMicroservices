using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using System;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("38250494-778F-4D03-A9E9-F1C17D851AE4")]
    public class MtlsMeshDiagnostics : Command
    {
        public MtlsMeshDiagnostics()
        {
            Instance = this;
        }

        public static MtlsMeshDiagnostics Instance { get; private set; }

        public override string EnglishName => "MtlsMeshDiagnostics";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Mesh mesh;
            Getter.GetMesh("Select mesh",out mesh);

            try
            {
                var results = MeshDiagnostics.GetMeshDiagnostics(mesh);

                RhinoApp.WriteLine($"NumberOfInvertedNormal = {results.NumberOfInvertedNormal}");
                RhinoApp.WriteLine($"NumberOfBadEdges = {results.NumberOfBadEdges}");
                RhinoApp.WriteLine($"NumberOfBadContours = {results.NumberOfBadContours}");
                RhinoApp.WriteLine($"NumberOfNearBadEdges = {results.NumberOfNearBadEdges}");
                RhinoApp.WriteLine($"NumberOfHoles = {results.NumberOfHoles}");
                RhinoApp.WriteLine($"NumberOfShells = {results.NumberOfShells}");
                RhinoApp.WriteLine($"NumberOfOverlappingTriangles = {results.NumberOfOverlappingTriangles}");
                RhinoApp.WriteLine($"NumberOfIntersectingTriangles = {results.NumberOfIntersectingTriangles}");

                return Result.Success;
            }
            catch (Exception e)
            {
                RhinoApp.WriteLine(e.Message);
            }

            return Result.Failure;
        }
    }
}
