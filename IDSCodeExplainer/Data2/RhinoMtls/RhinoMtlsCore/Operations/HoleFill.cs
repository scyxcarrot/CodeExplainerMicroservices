using MtlsIds34.Array;
using MtlsIds34.MeshFix;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class HoleFill
    {
        [HandleProcessCorruptedStateExceptions]
        public static bool PerformNormalHoleFill(Mesh targetmesh, long[,] borderSegments, out Mesh meshFilled)
        {
            var input = targetmesh.SimplifyMesh();

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var fillHoles = new FillHoles()
                {
                    Method = FillHolesMethod.Holetriangulation
                };
                fillHoles.Triangles = input.Faces.ToArray2D(context);
                fillHoles.Vertices = input.Vertices.ToArray2D(context);
                fillHoles.HolesSegments = Array2D.Create(context, borderSegments);

                try
                {
                    var result = fillHoles.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    meshFilled = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);

                    return true;
                }
                catch (Exception e)
                {
                    throw new MtlsException("FillHoles", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static bool FindBorderVertexHoleSegments(Mesh targetmesh, long borderVertex, out long[,] segment)
        {
            var input = targetmesh.SimplifyMesh();

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var findHoles = new FindBoundaryEdges();
                findHoles.Triangles = input.Faces.ToArray2D(context);
                findHoles.Vertices = input.Vertices.ToArray2D(context);

                try
                {
                    var resultFindHoles = findHoles.Operate(context);
                    var array = (long[,])resultFindHoles.EdgesSegments.Data;

                    segment = null;
                    var startIndex = 0;
                    var foundSegment = false;
                    for (int i = 0, j = 1; i <= array.GetUpperBound(0); i++, j++)
                    {
                        for (var k = 0; k <= array.GetUpperBound(1); k++)
                        {
                            if (array[i, k] == borderVertex)
                            {
                                foundSegment = true;
                            }
                        }
                        if (j == array.GetUpperBound(0) + 1 || array[i, 1] != array[j, 0])
                        {
                            if (foundSegment)
                            {
                                var length = j - startIndex;
                                segment = new long[length, 2];
                                Array.Copy(array, (startIndex * 2), segment, 0, length * 2);
                                break;
                            }
                            startIndex = j;
                        }
                    }

                    return foundSegment;
                }
                catch (Exception e)
                {
                    throw new MtlsException("FindBoundaryEdges", e.Message);
                }
            }
        }

        public static bool PerformFreeformHoleFill(Mesh targetmesh, long[,] borderSegments, bool tangent, bool treatAsOneHole, double gridSize, out Mesh meshFilled)
        {
            Mesh[] surfacesDummy;
            return PerformFreeformHoleFill(targetmesh, borderSegments, tangent, treatAsOneHole, gridSize, out meshFilled, out surfacesDummy);
        }

        [HandleProcessCorruptedStateExceptions]
        public static bool PerformFreeformHoleFill(Mesh targetmesh, long[,] borderSegments, bool tangent, bool treatAsOneHole, double gridSize, out Mesh meshFilled, out Mesh[] meshFilledSurfaces)
        {
            var input = targetmesh.DuplicateMesh();

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var vertices = targetmesh.Vertices.ToDouble2DArray();
                var toCurvesResult = Curves.ToCurvesInternal(context, vertices, borderSegments);
                var indices = (long[])toCurvesResult.PointIndices.Data;
                var ranges = (long[,])toCurvesResult.Ranges.Data;

                var fillHoles = new FillHolesFreeform()
                {
                    Tangent = tangent,
                    TreatAsOneHole = treatAsOneHole,
                    GridSize = gridSize
                };
                fillHoles.Triangles = input.Faces.ToArray2D(context);
                fillHoles.Vertices = input.Vertices.ToArray2D(context);
                fillHoles.HolesVertexIndices = Array1D.Create(context, indices);
                fillHoles.HolesRanges = Array2D.Create(context, ranges);

                try
                {
                    var result = fillHoles.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    meshFilled = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray, false);

                    var surfaceData = (ulong[])result.SurfaceStructure.Data;

                    var ids = MeshUtilities.GetSurfaceStructureIndexes(result.SurfaceStructure);
                    meshFilledSurfaces = new Mesh[ids.Length];

                    for (var i = 0; i < ids.Length; ++i)
                    {
                        meshFilledSurfaces[i] = MeshUtilities.GetSubSurface(meshFilled, surfaceData, ids[i]);
                    }

                    meshFilled.Faces.CullDegenerateFaces();

                    return true;
                }
                catch (Exception e)
                {
                    throw new MtlsException("FillHolesFreeform", e.Message);
                }
            }
        }
    }
}