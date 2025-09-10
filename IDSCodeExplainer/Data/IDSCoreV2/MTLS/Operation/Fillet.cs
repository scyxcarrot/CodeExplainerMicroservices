using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public class Fillet
    {
        [HandleProcessCorruptedStateExceptions]
        public static bool PerformFillet(IConsole console, IMesh referenceMesh, IMesh meshToPerformFillet, double radius, double tolerance, out IMesh filletedMesh)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var splitBySmoothSurfaceResult = new MtlsIds34.MeshMark.SplitBySmoothSurface()
                    {
                        Triangles = Array2D.Create(context, referenceMesh.Faces.ToFacesArray2D()),
                        Vertices = Array2D.Create(context, referenceMesh.Vertices.ToVerticesArray2D()),
                        MaxDihedralAngle = 30.0
                    }.Operate(context);

                    var autoFixResult = new MtlsIds34.MeshFix.AutoFix()
                    {
                        Triangles = Array2D.Create(context, referenceMesh.Faces.ToFacesArray2D()),
                        Vertices = Array2D.Create(context, referenceMesh.Vertices.ToVerticesArray2D()),
                        SurfaceStructure = splitBySmoothSurfaceResult.SurfaceStructure,
                        Method = MtlsIds34.MeshFix.AutoFixMethod.Basic
                    }.Operate(context);

                    autoFixResult = new MtlsIds34.MeshFix.AutoFix()
                    {
                        Vertices = autoFixResult.Vertices,
                        Triangles = autoFixResult.Triangles,
                        SurfaceStructure = autoFixResult.SurfaceStructure,
                        Method = MtlsIds34.MeshFix.AutoFixMethod.FixOverlaps
                    }.Operate(context);

                    var surfaceDiagnosticsResult = new MtlsIds34.MeshInspect.SurfaceDiagnostics()
                    {
                        Vertices = autoFixResult.Vertices,
                        Triangles = autoFixResult.Triangles,
                        SurfaceStructure = autoFixResult.SurfaceStructure
                    }.Operate(context);

                    var areas = (double[])surfaceDiagnosticsResult.Areas.Data;
                    var indexOfLargestArea = -1;
                    var largestArea = 0.0;

                    for (var i = 0; i < areas.Length; i++)
                    {
                        var area = areas[i];
                        if (area > largestArea)
                        {
                            largestArea = area;
                            indexOfLargestArea = i;
                        }
                    }

                    var indexOfLargestSurface = ((ulong[])surfaceDiagnosticsResult.LabelToIndex.Data)[indexOfLargestArea];
                    var surfaceStructure = (ulong[])autoFixResult.SurfaceStructure.Data;
                    var triangleCount = surfaceStructure.Count(i => i == indexOfLargestSurface);

                    const int verticesPerFace = 3;
                    var intArray = new ulong[triangleCount, verticesPerFace];
                    var counter = 0;

                    var triangles = (ulong[,])autoFixResult.Triangles.Data;
                    for (ulong i = 0; i < (ulong)surfaceStructure.Length; i++)
                    {
                        if (surfaceStructure[i] == indexOfLargestSurface)
                        {
                            intArray[counter, 0] = triangles[i, 0];
                            intArray[counter, 1] = triangles[i, 1];
                            intArray[counter, 2] = triangles[i, 2];
                            counter++;
                        }
                    }

                    var faces = Array2D.Create(context, intArray);

                    var findHoleBordersResult = new MtlsIds34.MeshInspect.FindHoleBorders
                    {
                        Triangles = faces,
                        Vertices = autoFixResult.Vertices
                    }.Operate(context);

                    var vertices = (double[,])autoFixResult.Vertices.Data;
                    var vertexIndices = (long[])findHoleBordersResult.BorderVertexIndices.Data;
                    var curvePointArray = new double[vertexIndices.Length, 3];

                    for (var i = 0; i < vertexIndices.Length; i++)
                    {
                        var vertexIndex = vertexIndices[i];

                        curvePointArray[i, 0] = vertices[vertexIndex, 0];
                        curvePointArray[i, 1] = vertices[vertexIndex, 1];
                        curvePointArray[i, 2] = vertices[vertexIndex, 2];
                    }

                    var attractToMeshResult = new MtlsIds34.Curve.AttractToMesh()
                    {
                        Triangles = Array2D.Create(context, meshToPerformFillet.Faces.ToFacesArray2D()),
                        Vertices = Array2D.Create(context, meshToPerformFillet.Vertices.ToVerticesArray2D()),
                        CurvePoints = Array2D.Create(context, curvePointArray),
                        CurveRanges = findHoleBordersResult.BorderRanges
                    }.Operate(context);

                    var filletResult = new MtlsIds34.MeshFinish.Fillet()
                    {
                        Triangles = attractToMeshResult.Triangles,
                        Vertices = attractToMeshResult.Vertices,
                        FilletVertexIndices = attractToMeshResult.CurveIndices,
                        FilletRanges = attractToMeshResult.CurveRanges,
                        DataType = MtlsIds34.MeshFinish.FilletInputDataType.Architectural,
                        FilletRadius = radius,
                        Tolerance = tolerance,
                    }.Operate(context);

                    var vertexArray = (double[,])filletResult.Vertices.Data;
                    var triangleArray = (ulong[,])filletResult.Triangles.Data;
                    
                    filletedMesh = new IDSMesh(vertexArray, triangleArray);

                    TriangleSurfaceDistanceV2.DistanceBetween(console,
                        meshToPerformFillet.Faces.ToFacesArray2D(),
                        meshToPerformFillet.Vertices.ToVerticesArray2D(),
                        filletedMesh.Faces.ToFacesArray2D(),
                        filletedMesh.Vertices.ToVerticesArray2D(),
                        out var vertexDistances, out var triangleCenterDistances);

                    if (vertexDistances.Max() < 0.01 && triangleCenterDistances.Max() < 0.01)
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    throw new MtlsException("Fillet", e.Message);
                }
            }
        }
    }
}
