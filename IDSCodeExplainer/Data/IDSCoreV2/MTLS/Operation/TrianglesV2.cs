using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.MeshFix;
using MtlsIds34.MeshInspect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class TrianglesV2
    {
        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformFilterSharpTriangles(IConsole console, IMesh inmesh, double widthThreshold, double angleThreshold)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var op = new FilterSharpTriangles
                {
                    Triangles = Array2D.Create(context, inmesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, inmesh.Vertices.ToVerticesArray2D()),
                    WidthThreshold = widthThreshold,
                    AngleThreshold = angleThreshold,
                    Action = FilterSharpTrianglesAction.CollapseTriangles
                };

                try
                {
                    var result = op.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("FilterSharpTriangles", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformRemoveOverlappingTriangles(IConsole console, IMesh inmesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var triangleArray = inmesh.Faces.ToFacesArray2D();
                var vertexArray = inmesh.Vertices.ToVerticesArray2D();

                var overlappingTriangles = new OverlappingTriangles
                {
                    Triangles = Array2D.Create(context, triangleArray),
                    Vertices = Array2D.Create(context, vertexArray),
                };

                //parameter values are default values for fix wizard 3-matic Medical 15
                var angleToleranceInDegrees = 5.0000;
                overlappingTriangles.AngleTolerance = angleToleranceInDegrees;
                overlappingTriangles.DistanceTolerance = 0.1000;
                overlappingTriangles.CheckFaceToBack = true;
                overlappingTriangles.CheckFaceToFace = true;

                try
                {
                    var result = overlappingTriangles.Operate(context);

                    if (result.OverlappingTriangles != null)
                    {
                        var indexes = (long[])result.OverlappingTriangles.Data;
                        if (indexes.Length > 0)
                        {
                            var triangleIndexes = indexes.Select(i => (int)i);
                            var triangles = new List<IFace>(inmesh.Faces.Where((tri, index) => !triangleIndexes.Contains(index)));
                            triangleArray = triangles.ToFacesArray2D();
                        }
                    }

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("RemoveOverlappingTriangles", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformRemoveIntersectingTriangles(IConsole console, IMesh inmesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var triangleArray = inmesh.Faces.ToFacesArray2D();
                var vertexArray = inmesh.Vertices.ToVerticesArray2D();

                try
                {
                    var result = MeshDiagnostics.GetDiagnostics(console, context, inmesh);

                    if (result.IntersectingTriangles != null)
                    {
                        var indexes = (long[])result.IntersectingTriangles.Data;
                        if (indexes.Length > 0)
                        {
                            var triangleIndexes = indexes.Select(i => (int)i);
                            var triangles = new List<IFace>(inmesh.Faces.Where((tri, index) => !triangleIndexes.Contains(index)));
                            triangleArray = triangles.ToFacesArray2D();
                        }
                    }

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("RemoveIntersectingTriangles", e.Message);
                }
            }
        }

        public static IMesh CullDegeneratedFaces(IConsole console, IMesh inmesh)
        {
            DiagnosticsResult result;
            var triangleArray = inmesh.Faces.ToFacesArray2D();
            var vertexArray = inmesh.Vertices.ToVerticesArray2D();

            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    result = MeshDiagnostics.GetDiagnostics(console, context, inmesh);
                    if (result.DegenerateTriangles != null)
                    {
                        var indexes = (long[])result.DegenerateTriangles.Data;
                        if (indexes.Length > 0)
                        {
                            var triangleIndexes = indexes.Select(i => (int)i);
                            var triangles = new List<IFace>(inmesh.Faces.Where((tri, index) => !triangleIndexes.Contains(index)));
                            triangleArray = triangles.ToFacesArray2D();
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new MtlsException("CullDegeneratedTriangles", e.Message);
                }
            }

            return new IDSMesh(vertexArray, triangleArray);
        }

        public static IMesh CombineAndCompactIdenticalVertices(IConsole console, IMesh inmesh)
        {
            return StitchV2.PerformStitchVertices(console, inmesh);
        }
    }
}