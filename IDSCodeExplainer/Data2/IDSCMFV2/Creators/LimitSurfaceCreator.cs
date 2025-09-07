using IDS.CMF.V2.Utilities;
using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.V2.Creators
{
    public class LimitSurfaceCreator
    {
        private const double DefaultExtensionLength = 10.0;

        private readonly IConsole _console;

        private IVector3D _averageNormal;

        public IMesh CastPart { get; set; }
        public IMesh CreatedMesh { get; private set; }
        public List<IPoint3D> OriginalCurvePoints { get; private set; }
        public List<IPoint3D> OuterCurvePoints { get; set; }
        public bool IsSuccessful { get; private set; }

        public LimitSurfaceCreator(IConsole console)
        {
            _console = console;
            OriginalCurvePoints = new List<IPoint3D>();
            OuterCurvePoints = new List<IPoint3D>();
            CreatedMesh = new IDSMesh();
            IsSuccessful = false;
        }

        public bool CreateLimitSurfacesAsMesh(List<IPoint3D> points, double extensionLength = DefaultExtensionLength)
        {
            IsSuccessful = false;

            var processingSuccess = ProcessCurvePoints(points, extensionLength);

            if (!processingSuccess)
            {
                return false;
            }

            var outerMesh = CreateOuterMeshOnly(OriginalCurvePoints, OuterCurvePoints);

            if (outerMesh != null)
            {
                CreatedMesh = ProcessMeshPipeline(outerMesh);
                IsSuccessful = true;
                return true;
            }
            return false;
        }

        private IMesh ProcessMeshPipeline(IMesh idsMesh)
        {
            CalculateAverageNormal(idsMesh);

            var meshOperations = new Func<IMesh, IMesh>[]
            {
                mesh => AutoFixV2.PerformFillHoles(_console, mesh),
                mesh => MeshUtilitiesV2.CorrectMeshNormalDirection(_console, CastPart, mesh, _averageNormal),
                mesh => RemeshV2.PerformRemesh(_console, mesh, 0.0, 3.0, 0.3, 0.2, 0.3, true, 3),
                mesh => AutoFixV2.SmoothMesh(_console, mesh, 30),
                mesh => RemeshV2.PerformRemesh(_console, mesh, 0.0, 0.3, 0.3, 0.02, 0.3, true, 3),
                mesh => AutoFixV2.SmoothMesh(_console, mesh, 300)
            };
            var currentMesh = idsMesh;
            foreach (var operation in meshOperations)
            {
                currentMesh = ApplyMeshOperation(currentMesh, operation);
            }
            return currentMesh;
        }

        private static IMesh ApplyMeshOperation(IMesh inputMesh, Func<IMesh, IMesh> operation)
        {
            var result = operation(inputMesh);
            var isValid = result?.Vertices != null && result.Faces != null;
            return isValid ? result : inputMesh;
        }

        private void CalculateAverageNormal(IMesh mesh)
        {
            var idsMeshWithNormal = MeshNormal.PerformNormal(_console, mesh);
            var sumNormal = idsMeshWithNormal.TriangleNormals.Aggregate(new IDSVector3D(), (sum, normal) => new IDSVector3D
            {
                X = sum.X + normal.X,
                Y = sum.Y + normal.Y,
                Z = sum.Z + normal.Z
            });
            _averageNormal = sumNormal.Div(idsMeshWithNormal.TriangleNormals.Count);
            _averageNormal.Unitize(); // Get unit vector of average normal
        }

        private IMesh CreateOuterMeshOnly(List<IPoint3D> originalPoints, List<IPoint3D> outerPoints)
        {
            var originalCurve = new IDSCurve(originalPoints);
            var outerCurve = new IDSCurve(outerPoints);
            return Curves.TriangulateFullyBetweenCurves(_console, originalCurve, outerCurve);
        }

        private bool ProcessCurvePoints(List<IPoint3D> inputPoints, double extensionLength)
        {
            // Initial resampling to prepare for frame calculation
            var resampledForFrames = Curves.GetEquidistantPointsOnCurve(_console, new IDSCurve(inputPoints), 0.0);
            if (resampledForFrames.Count < 2)
            {
                return false;
            }

            LimitSurfaceUtilities.GenerateCurveExtensionPoints(
                _console,
                resampledForFrames,
                extensionLength,
                out var originalCurvePoints,
                out var outerCurvePoints);

            if (originalCurvePoints.Count < 2 || outerCurvePoints.Count < 2)
            {
                return false;
            }

            // Process outer curve points
            var resampledOuterCurve = Curves.GetEquidistantPointsOnCurve(_console, new IDSCurve(outerCurvePoints), 0.0);
            var smoothedOuter = Curves.SmoothCurve(_console, new IDSCurve(resampledOuterCurve), 1, 1);

            // Process original (inner) curve points
            var resampledInnerCurve = Curves.GetEquidistantPointsOnCurve(_console, new IDSCurve(originalCurvePoints), 0.05);
            var smoothedInner = Curves.SmoothCurve(_console, new IDSCurve(resampledInnerCurve), 1, 1);

            // Convert final results to output format
            // store the points for further processing
            OriginalCurvePoints = smoothedInner;
            OuterCurvePoints = smoothedOuter;

            return true;
        }
    }
}