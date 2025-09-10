using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Utilities;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.Core;
using MtlsIds34.Core.Primitives;
using MtlsIds34.Curve;
using MtlsIds34.Geometry;
using MtlsIds34.Lattice;
using MtlsIds34.Math;
using MtlsIds34.MeshDesign;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class Curves
    {
        [HandleProcessCorruptedStateExceptions]
        public static List<IPoint3D> ClosestPoints(IConsole console, ICurve curve, List<IPoint3D> points)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var operation = new ClosestPoint()
                    {
                        Points = Array2D.Create(context, curve.Points.ToPointsArray2D()),
                        QueryPoints = Array2D.Create(context, points.ToPointsArray2D()),
                    };

                    var output = operation.Operate(context);

                    if (output.Points == null)
                    {
                        return null;
                    }

                    var pointsArray = (double[,])output.Points.Data;
                    return pointsArray.ToPointsList();
                }
                catch (Exception e)
                {
                    throw new MtlsException("ClosestPoint", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static List<ICurve> ShatterPolyline(IConsole console, ICurve curve, List<IPoint3D> divisionPoints, double tolerance)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var operation = new ShatterPolyline
                {
                    Points = Array2D.Create(context, curve.Points.ToPointsArray2D()),
                    DivisionPoints = Array2D.Create(context, divisionPoints.ToPointsArray2D()),
                    Tolerance = tolerance
                };

                try
                {
                    var output = operation.Operate(context);
                    var curvePoints = (double[,])output.Points.Data;
                    var curveRanges = (long[,])output.Ranges.Data;

                    var totalCurves = curveRanges.GetLength(0);

                    var curves = new List<ICurve>();

                    for (var i = 0; i < totalCurves; i++)
                    {
                        var points = new List<IPoint3D>();
                        var startIndex = (int)curveRanges[i, 0];
                        var endIndex = (int)curveRanges[i, 1];

                        for (var j = startIndex; j < endIndex; j++)
                        {
                            points.Add(new IDSPoint3D(curvePoints[j, 0], curvePoints[j, 1], curvePoints[j, 2]));
                        }

                        var shatterCurve = new IDSCurve(points);
                        curves.Add(shatterCurve);
                    }

                    return curves;
                }
                catch (Exception e)
                {
                    throw new MtlsException("ShatterPolyline", e.Message);
                }
            }
        }

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
        public static List<ICurve> IntersectionCurve(IConsole console, IMesh mesh1, IMesh mesh2)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var operation = new IntersectionsMeshAndMesh()
                    {
                        Triangles1 = Array2D.Create(context, mesh1.Faces.ToFacesArray2D()),
                        Vertices1 = Array2D.Create(context, mesh1.Vertices.ToVerticesArray2D()),
                        Triangles2 = Array2D.Create(context, mesh2.Faces.ToFacesArray2D()),
                        Vertices2 = Array2D.Create(context, mesh2.Vertices.ToVerticesArray2D())
                    };

                    var output = operation.Operate(context);

                    if (output.Points == null || output.Segments == null)
                    {
                        return new List<ICurve>();
                    }

                    var vertices = (double[,])output.Points.Data;
                    var segments = (long[,])output.Segments.Data;
                    return CreateCurvesBySegments(console, vertices, segments, context);
                }
                catch (Exception e)
                {
                    throw new MtlsException("IntersectionCurve", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        private static List<ICurve> CreateCurvesBySegments(IConsole console, double[,] vertices, long[,] segments, Context context)
        {
            var operation = new ToCurves();
            operation.Points = Array2D.Create(context, vertices);
            operation.Segments = Array2D.Create(context, segments);

            var output = operation.Operate(context);
            var curvePoints = (double[,])output.Points.Data;
            var curveRanges = (long[,])output.Ranges.Data;

            var totalCurves = curveRanges.GetLength(0);

            var curves = new List<ICurve>();

            for (var i = 0; i < totalCurves; i++)
            {
                var points = new List<IPoint3D>();
                var startIndex = (int)curveRanges[i, 0];
                var endIndex = (int)curveRanges[i, 1];

                for (var j = startIndex; j < endIndex; j++)
                {
                    points.Add(new IDSPoint3D(curvePoints[j, 0], curvePoints[j, 1], curvePoints[j, 2]));
                }

                var attracted = new IDSCurve(points);
                curves.Add(attracted);
            }

            return curves;
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh ExtrudeCurve(IConsole console, ICurve curve, IVector3D direction, double distance)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var operation = new ExtrudeCurve()
                    {
                        Points = Array2D.Create(context, curve.Points.ToPointsArray2D()),
                        //Ranges not needed as it is a single curve
                        Direction = new Vector3(direction.X, direction.Y, direction.Z),
                        Distance = distance
                    }.Operate(context);

                    var vertexArray = (double[,])operation.Vertices.Data;
                    var triangleArray = (ulong[,])operation.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("ExtrudeCurve", e.Message);
                }
            }
        }

        //Curve should be located in the XY plane at Z=0
        public static IMesh GeneratePolygon(IConsole console, ICurve curve, IPoint3D center, double height)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var extrudeCurveResult = new ExtrudeCurve()
                    {
                        Points = Array2D.Create(context, curve.Points.ToPointsArray2D()),
                        //Ranges not needed as it is a single curve
                        Direction = new Vector3(0.0, 0.0, 1.0),
                        Distance = height
                    }.Operate(context);

                    var fillHolesResult = new MtlsIds34.MeshFix.AutoFix()
                    {
                        Triangles = extrudeCurveResult.Triangles,
                        Vertices = extrudeCurveResult.Vertices,
                        Method = MtlsIds34.MeshFix.AutoFixMethod.FillHoles
                    }.Operate(context);

                    var transformationResult = new TransformationFromPlaneAlignment()
                    {
                        FromOrigin = new Vector3(0.0, 0.0, height / 2),
                        FromNormal = new Vector3(1.0, 1.0, 0.0),
                        ToOrigin = new Vector3(0.0, 0.0, 0.0),
                        ToNormal = new Vector3(1.0, 1.0, 0.0),
                    }.Operate(context);

                    var transformPolygonResult = new MtlsIds34.MeshDesign.Transform()
                    {
                        Vertices = fillHolesResult.Vertices,
                        Triangles = fillHolesResult.Triangles,
                        Transformation = transformationResult.Transformation
                    }.Operate(context);

                    var transformationResult1 = new TransformationFromPlaneAlignment()
                    {
                        FromOrigin = new Vector3(0.0, 0.0, 0.0),
                        FromNormal = new Vector3(1.0, 1.0, 0.0),
                        ToOrigin = new Vector3(center.X, center.Y, center.Z),
                        ToNormal = new Vector3(1.0, 1.0, 0.0),
                    }.Operate(context);

                    var transformPolygonResult1 = new MtlsIds34.MeshDesign.Transform()
                    {
                        Vertices = transformPolygonResult.Vertices,
                        Triangles = transformPolygonResult.Triangles,
                        Transformation = transformationResult1.Transformation
                    }.Operate(context);

                    var vertexArray = (double[,])transformPolygonResult1.Vertices.Data;
                    var triangleArray = (ulong[,])transformPolygonResult1.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("GeneratePolygon", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static List<ICurve> IntersectionsMeshAndPlane(IConsole console, IMesh mesh, IPoint3D planeOrigin, IVector3D planeDirection)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var operation = new IntersectionsMeshAndPlanes()
                    {
                        Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                        Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                        Origin = new Vector3(planeOrigin.X, planeOrigin.Y, planeOrigin.Z),
                        Normal = new Vector3(planeDirection.X, planeDirection.Y, planeDirection.Z)
                    };

                    var output = operation.Operate(context);

                    if (output.CutPoints == null || output.CutSegments == null)
                    {
                        return new List<ICurve>();
                    }

                    var vertices = (double[,])output.CutPoints.Data;
                    var segments = (long[,])output.CutSegments.Data;
                    return CreateCurvesBySegments(console, vertices, segments, context);
                }
                catch (Exception e)
                {
                    throw new MtlsException("IntersectionsMeshAndPlanes", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IPoint3D ClosestPoint(IConsole console, ICurve curve, IPoint3D point)
        {
            return ClosestPoints(console, curve, new List<IPoint3D> { point })[0];
        }

        [HandleProcessCorruptedStateExceptions]
        public static List<IMesh> SplitWithCurve(IConsole console, IMesh inputMesh, ICurve curve, out IMesh meshWithSurfaceStructure, out ulong[] surfaceStructure)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var splitByCurve = new SplitByCurve()
                {
                    Triangles = Array2D.Create(context, inputMesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, inputMesh.Vertices.ToVerticesArray2D()),
                    CurvePoints = Array2D.Create(context, curve.Points.ToPointsArray2D())
                };
                //CurveSegments is an optional field. SplitByCurve works for disjointed curves even without having this field.

                try
                {
                    var result = splitByCurve.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;
                    var resultMesh = new IDSMesh(vertexArray, triangleArray);
                    meshWithSurfaceStructure = new IDSMesh(resultMesh);

                    var splitStructure = (ulong[])result.SplitStructure.Data;
                    surfaceStructure = splitStructure;
                    var parts = MeshUtilitiesV2.GetSurfaces(resultMesh, splitStructure);
                    return parts;
                }
                catch (Exception e)
                {
                    throw new MtlsException("SplitByCurve", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static ICurve AttractCurve(IConsole console, IMesh mesh, ICurve curve)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var operation = new MtlsIds34.Curve.AttractToMesh
                {
                    Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                    CurvePoints = Array2D.Create(context, curve.Points.ToPointsArray2D())
                };

                try
                {
                    var output = operation.Operate(context);
                    var vertices = (double[,])output.Vertices.Data;
                    var indexes = (long[])output.CurveIndices.Data;

                    var points = new List<IPoint3D>();
                    for (var j = 0; j < indexes.Length; j++)
                    {
                        var index = indexes[j];
                        points.Add(new IDSPoint3D(vertices[index, 0], vertices[index, 1], vertices[index, 2]));
                    }

                    return new IDSCurve(points);
                }
                catch (Exception e)
                {
                    throw new MtlsException("AttractToMesh", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static double GetCurveLength(IConsole console, ICurve curve)
        {
            return GetCurvesLength(console, new List<ICurve> { curve })[0];
        }

        [HandleProcessCorruptedStateExceptions]
        public static List<double> GetCurvesLength(IConsole console, List<ICurve> curves)
        {
            var curvePoints = new List<IPoint3D>();
            var curveRanges = new long[curves.Count, 2];

            for (var i = 0; i < curves.Count; i++)
            {
                var curve = curves[i];

                curveRanges[i, 0] = curvePoints.Count;
                curvePoints.AddRange(curve.Points);
                curveRanges[i, 1] = curvePoints.Count;
            }

            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var operation = new MtlsIds34.Curve.Dimensions
                {
                    Points = Array2D.Create(context, curvePoints.ToPointsArray2D()),
                    Ranges = Array2D.Create(context, curveRanges),
                };

                try
                {
                    var output = operation.Operate(context);
                    var lengths = (double[])output.Lengths.Data;
                    return lengths.ToList();
                }
                catch (Exception e)
                {
                    throw new MtlsException("Dimensions", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformTriangulateBetweenCurves(IConsole console,
            ICurve curve1, ICurve curve2,
            int[,] curve1Range, int[,] curve2Range)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var operation = new TriangulateBetweenCurves()
                    {
                        CurvePointsFirst = curve1.Points.Select(point => new Vector3(point.X, point.Y, point.Z)),
                        CurveRangesFirst = curve1Range,
                        CurvePointsSecond = curve2.Points.Select(point => new Vector3(point.X, point.Y, point.Z)),
                        CurveRangesSecond = curve2Range
                    };
                    var result = operation.Operate(context);

                    return new IDSMesh((double[,])result.Vertices.Data, (ulong[,])result.Triangles.Data);
                }
                catch (Exception exception)
                {
                    throw new MtlsException("PerformTriangulateBetweenCurves", exception.Message);
                }
            }
        }

        public static IMesh TriangulateFullyBetweenCurves(IConsole console,
            ICurve curve1, ICurve curve2)
        {
            var curve1Range = new int[,] { { 0, curve1.Points.Count } };
            var curve2Range = new int[,] { { 0, curve2.Points.Count } };

            return PerformTriangulateBetweenCurves(console, curve1, curve2, curve1Range, curve2Range);
        }

        [HandleProcessCorruptedStateExceptions]
        public static List<IPoint3D> GetEquidistantPointsOnCurve(
            IConsole console,
            ICurve curve, double stepSize)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var curve1Range = new int[,] { { 0, curve.Points.Count } };
                    var operation = new Resample()
                    {
                        Points = Array2D.Create(
                            context, curve.Points.ToPointsArray2D()),
                        Ranges = curve1Range,
                        ResampleStep = stepSize
                    };
                    var result = operation.Operate(context);
                    var pointsDouble = (double[,])result.Points.Data;

                    var points = new List<IPoint3D>();
                    for (var index = 0;
                         index < pointsDouble.GetLength(0);
                         index++)
                    {
                        points.Add(new IDSPoint3D(
                            pointsDouble[index, 0],
                            pointsDouble[index, 1],
                            pointsDouble[index, 2]));
                    }

                    return points;
                }
                catch (Exception exception)
                {
                    throw new MtlsException("GetEquidistantPointsOnCurve", exception.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static List<KeyValuePair<IPoint3D, List<IVector3D>>> LocalFrames(IConsole console, ICurve curve)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var points = curve.Points.ToPointsArray2D();
                    var operation = new LocalFrames
                    {
                        Points = Array2D.Create(context, curve.Points.ToPointsArray2D()),
                        Ranges = new long[,] { { 0, curve.Points.Count } },
                        OffsetDir = 0
                    };
                    var output = operation.Operate(context);
                    var frameOutput = Array3D.GetTArray<double>(output.Frames);
                    // 1st column (tangent)
                    // 2nd column (normal)
                    // 3rd column (binormal)
                    // 4th column (position)
                    var framesData = new List<KeyValuePair<IPoint3D, List<IVector3D>>>();
                    var numOfFrames = frameOutput.GetLength(0);
                    // original points array to get positions
                    var pointIndex = 0;
                    // Process all frames from the operation output
                    for (var i = 0; i < numOfFrames; i++)
                    {
                        // Extract position directly from input points array
                        var framePosition = new IDSPoint3D(
                            points[pointIndex, 0],
                            points[pointIndex, 1],
                            points[pointIndex, 2]
                        );

                        // Create list of three vectors for this frame
                        var frameVectors = new List<IVector3D>();
                        for (int axis = 0; axis < 3; axis++)
                        {
                            var directionVector = new IDSVector3D(
                                frameOutput[i, axis, 0],
                                frameOutput[i, axis, 1],
                                frameOutput[i, axis, 2]
                            );
                            frameVectors.Add(directionVector);
                        }
                        // Create the inner dictionary for this frame
                        var inner = new KeyValuePair<IPoint3D, List<IVector3D>>(framePosition, frameVectors);
                        framesData.Add(inner);
                        pointIndex++;
                    }
                    return framesData;
                }
                catch (Exception e)
                {
                    throw new MtlsException("LocalFrames", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static List<IPoint3D> SmoothCurve(IConsole console, ICurve curve, double smoothingFactor, int iterations)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var operation = new Smooth()
                    {
                        Points = Array2D.Create(context, curve.Points.ToPointsArray2D()),
                        Ranges = new int[,] { { 0, curve.Points.Count } },
                        NumberOfIterations = iterations,
                        SmoothingFactor = smoothingFactor,
                    };

                    var result = operation.Operate(context);
                    var pointsDouble = (double[,])result.Points.Data;

                    var points = new List<IPoint3D>();
                    for (var index = 0;
                         index < pointsDouble.GetLength(0);
                         index++)
                    {
                        points.Add(new IDSPoint3D(
                            pointsDouble[index, 0],
                            pointsDouble[index, 1],
                            pointsDouble[index, 2]));
                    }

                    return points;
                }
                catch (Exception exception)
                {
                    throw new MtlsException("SmoothCurve", exception.Message);
                }
            }
        }
    }
}
