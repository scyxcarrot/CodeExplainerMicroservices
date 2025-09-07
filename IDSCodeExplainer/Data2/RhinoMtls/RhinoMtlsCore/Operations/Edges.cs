using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class Edges
    {
        [HandleProcessCorruptedStateExceptions]
        public static long[,] GetEdgeIndices(Mesh mesh)
        {
            using (var context = MtlsIds34Globals.CreateContext())
            {
                var findBoundary = new MtlsIds34.MeshFix.FindBoundaryEdges();
                findBoundary.Vertices = mesh.Vertices.ToArray2D(context);
                findBoundary.Triangles = mesh.Faces.ToArray2D(context);

                try
                {
                    var edges = findBoundary.Operate(context);

                    var edgeInts = (long[,])edges.EdgesSegments.Data;

                    return edgeInts;
                }
                catch (Exception e)
                {
                    throw new MtlsException("FindBoundaryEdges", e.Message);
                }
            }
        }

        public static bool HasOnlyOneBoundaryNakedBoundary(Mesh mesh)
        {
            var edgeInts = GetEdgeIndices(mesh);

            for (var i = 0; i < edgeInts.GetLength(0) - 1; i++)
            {
                if (edgeInts[i, 1] != edgeInts[i + 1, 0])
                {
                    return false;
                }
            }
            
            // Last one
            if (edgeInts[0, 0] != edgeInts[edgeInts.GetLength(0) - 1, 1])
            {
                return false;
            }

            return true;
        }

        public static double[,] GetEdgePointsAsDoubles(Mesh mesh)
        {
            var edgePoints = GetEdgePoints(mesh);
            var edgePointsAsDoubles = new double[edgePoints.GetLength(0), 3];

            var i = 0;
            foreach (var edgePoint in edgePoints)
            {
                edgePointsAsDoubles[i, 0] = edgePoint.X;
                edgePointsAsDoubles[i, 1] = edgePoint.Y;
                edgePointsAsDoubles[i, 2] = edgePoint.Z;
                i++;
            }

            return edgePointsAsDoubles;
        }

        public static Point3d[] GetEdgePoints(Mesh mesh)
        {
            var edgeInts = GetEdgeIndices(mesh);
            var edgeIntsUnique = edgeInts.Cast<long>().ToArray();
            var vertDoubles = mesh.Vertices.ToDouble2DArray();
            var edgePoints = new Point3d[edgeInts.GetLength(0)];

            // Add first point of every edge
            for (var i = 0; i < edgeInts.GetLength(0); i++)
            {
                var index = i * 2;
                edgePoints[i].X = vertDoubles[(int)edgeIntsUnique[index], 0];
                edgePoints[i].Y = vertDoubles[(int)edgeIntsUnique[index], 1];
                edgePoints[i].Z = vertDoubles[(int)edgeIntsUnique[index], 2];
            }

            return edgePoints;
        }
    }
}
