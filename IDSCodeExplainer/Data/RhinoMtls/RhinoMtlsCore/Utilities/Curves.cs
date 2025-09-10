using MtlsIds34.Array;
using MtlsIds34.Lattice;
using Rhino.Collections;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Utilities
{
    public class Curves
    {
        public static void SampleCurve(Curve theCurve, out Point3dList curveEdges, int numberOfPoints = 100)
        {
            curveEdges = new Point3dList();
            for (var i = 0; i < numberOfPoints; i++)
            {
                curveEdges.Add(theCurve.PointAtNormalizedLength(i / (double)numberOfPoints));
            }
        }

        public static Curve AttractCurve(Mesh mesh, Curve curve, double maxChordLengthRatio, double maxGeometricalError)
        {
            var output = AttractCurveOperation(mesh, curve, maxChordLengthRatio, maxGeometricalError);

            var triangleSurfaceVertices = output.Vertices.ToDouble2DArray();
            var segments = (long[,])output.Segments.Data;
            var totalSegments = segments.GetLength(0);
            var points = new List<Point3d>();
            for (var i = 0; i < totalSegments; i++)
            {
                var index = (int)segments[i, 0];
                points.Add(new Point3d((float)triangleSurfaceVertices[index, 0], (float)triangleSurfaceVertices[index, 1], (float)triangleSurfaceVertices[index, 2]));
            }

            var lastIndex = totalSegments - 1;
            if (curve.IsClosed && Math.Abs(segments[lastIndex, 0] - segments[lastIndex, 1]) > 0.1)
            {
                var index = (int)segments[lastIndex, 1];
                points.Add(new Point3d((float)triangleSurfaceVertices[index, 0], (float)triangleSurfaceVertices[index, 1], (float)triangleSurfaceVertices[index, 2]));
            }

            var attracted = new PolylineCurve(points);
            return attracted;
        }

        public static List<Curve> AttractFreeCurve(Mesh mesh, Curve curve, double maxChordLengthRatio, double maxGeometricalError)
        {
            var output = AttractCurveOperation(mesh, curve, maxChordLengthRatio, maxGeometricalError);

            var triangleSurfaceVertices = output.Vertices.ToDouble2DArray();
            var segments = (long[,])output.Segments.Data; 
            var attractedCurves = CreateCurvesBySegments(triangleSurfaceVertices, segments);
            //output.Dispose();

            return attractedCurves;
        }

        public static double[,] PopulateSegments(int length, int startIndex, bool isClosed = true)
        {
            if (!isClosed)
            {
                length = length - 1;
            }

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

            if (isClosed)
            {
                segmentArray[length - 1, pointsForASegment - 1] = startIndex;
            }

            return segmentArray;
        }

        [HandleProcessCorruptedStateExceptions]
        public static List<Curve> CreateCurvesBySegments(double[,] vertices, long[,] segments)
        {
            using (var context = MtlsIds34Globals.CreateContext())
            {
                try
                {
                    var output = ToCurvesInternal(context, vertices, segments);
                    var curvePoints = (double[,])output.Points.Data;
                    var curveRanges = (long[,])output.Ranges.Data;

                    var totalCurves = curveRanges.GetLength(0);

                    var curves = new List<Curve>();

                    for (var i = 0; i < totalCurves; i++)
                    {
                        var points = new List<Point3d>();
                        var startIndex = (int)curveRanges[i, 0];
                        var endIndex = (int)curveRanges[i, 1];

                        for (var j = startIndex; j < endIndex; j++)
                        {
                            points.Add(new Point3d(curvePoints[j, 0], curvePoints[j, 1], curvePoints[j, 2]));
                        }

                        var attracted = new PolylineCurve(points);
                        curves.Add(attracted);
                    }

                    return curves;
                }
                catch (Exception e)
                {
                    throw new MtlsException("PolyLineSplit", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        internal static ToCurvesResult ToCurvesInternal(MtlsIds34.Core.Context context, double[,] vertices, long[,] segments)
        {
            var operation = new ToCurves();
            operation.Points = Array2D.Create(context, vertices);
            operation.Segments = Array2D.Create(context, segments);

            return operation.Operate(context);
        }

        [HandleProcessCorruptedStateExceptions]
        private static AttractToMeshResult AttractCurveOperation(Mesh mesh, Curve curve, double maxChordLengthRatio, double maxGeometricalError)
        {
            var curvePointArray = curve.ToDouble2DArray(maxChordLengthRatio, maxGeometricalError);
            var curveSegmentArray = PopulateSegments(curvePointArray.GetLength(0), 0, curve.IsClosed);

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var operation = new AttractToMesh();
                operation.Triangles = mesh.Faces.ToArray2D(context);
                operation.Vertices = mesh.Vertices.ToArray2D(context);
                operation.LatticePoints = Array2D.Create(context, curvePointArray);
                operation.LatticeSegments = Array2D.Create(context, curveSegmentArray);

                try
                {
                    var output = operation.Operate(context);
                    return output;
                }
                catch (Exception e)
                {
                    throw new MtlsException("AttractCurve", e.Message);
                }
            }
        }
    }
}