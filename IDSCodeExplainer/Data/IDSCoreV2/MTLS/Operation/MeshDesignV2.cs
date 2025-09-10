using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class MeshDesignV2
    {
        public static IMesh ExtrudeSurface(IConsole console,
            IMesh inputSurface,
            IVector3D direction,
            double distance)
        {
            // expect to have 1 border only
            var surfaceCurve = MeshDiagnostics.FindSurfaceBorders(console, inputSurface)[0];
            direction.Unitize();
            var curveMesh = Curves.ExtrudeCurve(
                console, surfaceCurve, direction, distance);

            // move a copy of the surface
            var offsetSurface = GeometryTransformation.PerformMeshTransform(
                console,
                inputSurface,
                direction,
                distance);

            // check if inputSurface normal is in same direction as given direction
            var inputSurfaceNormalResult = MeshNormal.PerformNormal(
                console, inputSurface);
            var averageNormal = VectorUtilitiesV2.CalculateAverageNormal(
                inputSurfaceNormalResult.TriangleNormals);

            var dotProductAverageAndDirection = averageNormal.DotMul(direction);

            IMesh appendedSurface;
            if (dotProductAverageAndDirection > 0)
            {
                // invert input surface
                var inputSurfaceInverted = AutoFixV2.InvertNormal(
                    console,
                    inputSurface);

                appendedSurface = MeshUtilitiesV2.AppendMeshes(
                    new[] { inputSurfaceInverted, curveMesh, offsetSurface });
            }
            else
            {
                // invert offset surface
                var offsetSurfaceInverted = AutoFixV2.InvertNormal(
                    console,
                    offsetSurface);

                appendedSurface = MeshUtilitiesV2.AppendMeshes(
                    new[] { inputSurface, curveMesh, offsetSurfaceInverted });
            }

            var extrudedSurface = AutoFixV2.PerformStitch(
                    console, appendedSurface);

            return extrudedSurface;
        }

        [HandleProcessCorruptedStateExceptions]
        private static IMesh Stitch(
            IConsole console,
            IMesh mesh,
            double tolerance)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var stitch = new MtlsIds34.MeshDesign.Stitch()
                {
                    Vertices = mesh.Vertices.ToVerticesArray2D(),
                    Triangles = mesh.Faces.ToFacesArray2D(),
                    Tolerance = tolerance,
                };

                try
                {
                    var result = stitch.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Stitch", e.Message);
                }
            }
        }

        public static IMesh Stitch(
            IConsole console,
            IMesh mesh,
            double tolerance,
            int iterations)
        {
            var outputMesh = mesh;
            for (var i = 0; i < iterations; i++)
            {
                outputMesh = Stitch(console, outputMesh, tolerance);
            }

            return outputMesh;
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh Offset(
            IConsole console,
            IMesh mesh,
            double offsetDistance,
            bool offsetInBothSides,
            double smallestDetail,
            bool reduce)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var offsetOperation = new MtlsIds34.MeshDesign.Offset()
                {
                    Vertices = mesh.Vertices.ToVerticesArray2D(),
                    Triangles = mesh.Faces.ToFacesArray2D(),
                    Algorithm = MtlsIds34.MeshDesign.OffsetAlgorithm.MarchingCubes,
                    OffsetDistance = offsetDistance,
                    OffsetInBothSides = offsetInBothSides,
                    Reduce = reduce,
                    SmallestDetail = smallestDetail,
                };

                try
                {
                    var result = offsetOperation.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Offset", e.Message);
                }
            }
        }
    }
}