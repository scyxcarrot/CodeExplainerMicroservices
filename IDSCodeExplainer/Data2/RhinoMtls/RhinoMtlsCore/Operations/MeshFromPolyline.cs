using MtlsIds34.Array;
using MtlsIds34.Lattice;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class MeshFromPolyline
    {
        public static Mesh PerformMeshFromPolyline(Mesh mesh, double segmentRadius, double fractionalTriangleEdgeLength)
        {
            var vertices = mesh.Vertices;

            var lineList = new List<Line>();
            foreach (var face in mesh.Faces)
            {
                var lineAB = new Line(vertices[face.A], vertices[face.B]);
                var lineBC = new Line(vertices[face.B], vertices[face.C]);
                var lineCA = new Line(vertices[face.C], vertices[face.A]);
                lineList.Add(lineAB);
                lineList.Add(lineBC);
                lineList.Add(lineCA);
            }

            var points = GetPoints(lineList);
            var segments = GetSegments(lineList);
            lineList.Clear();

            return PerformMeshFromPolyline(points, segments, segmentRadius, fractionalTriangleEdgeLength);
        }

        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformMeshFromPolyline(double[,] points, ulong[,] segments, double segmentRadius, double fractionalTriangleEdgeLength)
        {
            using (var context = MtlsIds34Globals.CreateContext())
            {
                var operation = new ToMesh()
                {
                    SegmentRadius = segmentRadius,
                    FractionalTriangleEdgeLength = fractionalTriangleEdgeLength,
                    Mode =  ToMeshMode.Legacy
                };
                operation.Points = Array2D.Create(context, points);
                operation.Segments = Array2D.Create(context, segments);

                try
                {
                    var result = operation.Operate(context);
                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();
                    var polylineMesh = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);

                    return polylineMesh;
                }
                catch (Exception e)
                {
                    throw new MtlsException("FromPolyLine", e.Message);
                }
            }
        }

        public static Mesh PerformMeshFromPolyline(Brep brep, double segmentRadius, double fractionalTriangleEdgeLength)
        {
            var lineList = new List<Line>();
            foreach (var edge in brep.Edges)
            {
                var line = new Line(edge.PointAtStart, edge.PointAtEnd);
                lineList.Add(line);
            }

            var points = GetPoints(lineList);
            var segments = GetSegments(lineList);
            lineList.Clear();

            return PerformMeshFromPolyline(points, segments, segmentRadius, fractionalTriangleEdgeLength);
        }

        private static double[,] GetPoints(List<Line> lines)
        {
            const int coordinatesPerVertex = 3;
            var points = new double[lines.Count * 2, coordinatesPerVertex];

            for (var i = 0; i < lines.Count; i++)
            {
                points[i * 2, 0] = lines[i].FromX;
                points[i * 2, 1] = lines[i].FromY;
                points[i * 2, 2] = lines[i].FromZ;

                points[(i * 2) + 1, 0] = lines[i].ToX;
                points[(i * 2) + 1, 1] = lines[i].ToY;
                points[(i * 2) + 1, 2] = lines[i].ToZ;
            }

            return points;
        }

        private static ulong[,] GetSegments(List<Line> lines)
        {
            const int verticesPerLine = 2;
            var segments = new ulong[lines.Count, verticesPerLine];

            for (var i = 0; i < lines.Count; i++)
            {
                segments[i, 0] = (ulong)i * 2;
                segments[i, 1] = (ulong)(i * 2) + 1;
            }

            return segments;
        }
    }
}
