using IDS.CMF.DataModel;
using IDS.CMF.FileSystem;
using IDS.CMF.V2.Constants;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Utilities;
using Rhino;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Diagnostics;

namespace IDS.PICMF.Commands
{
    public abstract class CMFCreateSupportBase : CmfCommandBase
    {
        private const int _maxFixingIteration = 2;

        //change method to accept dataModel?
        protected virtual Mesh PerformFullyFixSupport(Mesh rawSupportMesh)
        {
            var timer = new Stopwatch();
            timer.Start();
            var resultantMesh = MeshFixingUtilities.PerformComplexFullyFix(rawSupportMesh, _maxFixingIteration, 
                ComplexFixingParameters.ComplexSharpTriangleWidthThreshold, ComplexFixingParameters.ComplexSharpTriangleAngleThreshold);
            timer.Stop();
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"It took {timer.ElapsedMilliseconds * 0.001} seconds to fix support.");
            AddTrackingParameterSafely("Fixing Support", StringUtilitiesV2.ElapsedTimeSpanToString(timer.Elapsed));

            return resultantMesh;
        }

        protected MeshDiagnostics.MeshDiagnosticsResult DisplayFinalResultDiagnostics(Mesh mesh)
        {
            var results = MeshDiagnostics.GetMeshDiagnostics(mesh);
            return DisplayFinalResultDiagnostics(results);
        }

        protected MeshDiagnostics.MeshDiagnosticsResult DisplayFinalResultDiagnostics(MeshDiagnostics.MeshDiagnosticsResult results)
        {
            RhinoApp.WriteLine();
            RhinoApp.WriteLine($"MeshDiagnostics: FinalResult");
            RhinoApp.WriteLine($"NumberOfInvertedNormal = {results.NumberOfInvertedNormal}");
            RhinoApp.WriteLine($"NumberOfBadEdges = {results.NumberOfBadEdges}");
            RhinoApp.WriteLine($"NumberOfBadContours = {results.NumberOfBadContours}");
            RhinoApp.WriteLine($"NumberOfNearBadEdges = {results.NumberOfNearBadEdges}");
            RhinoApp.WriteLine($"NumberOfHoles = {results.NumberOfHoles}");
            RhinoApp.WriteLine($"NumberOfShells = {results.NumberOfShells}");
            RhinoApp.WriteLine($"NumberOfOverlappingTriangles = {results.NumberOfOverlappingTriangles}");
            RhinoApp.WriteLine($"NumberOfIntersectingTriangles = {results.NumberOfIntersectingTriangles}");
            return results;
        }

        protected void ExportIntermediates(RhinoDoc doc, SupportCreationDataModel dataModel, string supportMeshType)
        {
#if (INTERNAL)
            var workingDir = DirectoryStructure.GetWorkingDir(doc);
            var exportDir = $@"{workingDir}\{supportMeshType.RemoveWhitespace()}MeshGeneration";

            StlUtilities.RhinoMesh2StlBinary(dataModel.InputRoI, $"{exportDir}\\InputRoI.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.WrapRoI1,
                $"{exportDir}\\WrapRoI1-GCD{dataModel.GapClosingDistanceForWrapRoI1}.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.WrapRoI2,
                $"{exportDir}\\WrapRoI2-S{dataModel.SkipWrapRoI2}.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.UnionedMesh, $"{exportDir}\\UnionedMesh.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.WrapUnion,
                $"{exportDir}\\WrapUnion-SD{dataModel.SmallestDetailForWrapUnion}.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.RemeshedMesh, $"{exportDir}\\RemeshedMesh.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.SmoothenMesh, $"{exportDir}\\SmoothenMesh.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.FinalResult, $"{exportDir}\\FinalResult.stl");
            StlUtilities.RhinoMesh2StlBinary(dataModel.FixedFinalResult, $"{exportDir}\\FixedFinalResult.stl");

            SystemTools.OpenExplorerInFolder(exportDir);

            RhinoApp.WriteLine("Intermediate part(s) were exported to the following folder:");
            RhinoApp.WriteLine("{0}", exportDir);
#endif
        }
    }
}
