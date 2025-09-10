using MtlsIds34.Array;
using MtlsIds34.Lattice;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class TubeFromPolyline
    {
        public static double[,] PopulateSegments(int length, int startIndex)
        {
            const int pointsForASegment = 2;
            var segmentArray = new double[length, pointsForASegment];
            var segment = startIndex;
            for (var i = 0; i < length; i++)
            {
                for (var j = 0; j < pointsForASegment; j++)
                {
                    segmentArray[i, j] = segment;
                    segment++;
                }
                segment--;
            }
            segmentArray[length - 1, pointsForASegment - 1] = startIndex;
            return segmentArray;
        }

        [HandleProcessCorruptedStateExceptions]
        public static bool PerformMeshFromPolyline(Curve curve, double segmentRadius, out Mesh meshTube)
        {
            var curvePointArray = curve.ToDouble2DArray(100, 0.01);
            var curveSegmentArray = PopulateSegments(curvePointArray.GetLength(0), 0);

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var operation = new ToMesh()
                {
                    SegmentRadius = segmentRadius,
                    FractionalTriangleEdgeLength = 0.3,
                    Mode = ToMeshMode.Legacy
                };
                operation.Points = Array2D.Create(context, curvePointArray);
                if (curve.IsClosed)
                {
                    operation.Segments = Array2D.Create(context, curveSegmentArray);
                }

                try
                {
                    var result = operation.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    meshTube = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);

                    return true;
                }
                catch (Exception e)
                {
                    throw new MtlsException("FromPolyLine", e.Message);
                }
            }
        }
    }
}
