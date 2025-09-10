using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Core.Primitives;
using MtlsIds34.Math;
using MtlsIds34.MeshDesign;
using MtlsIds34.Primitives;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Cylinder = MtlsIds34.Primitives.Cylinder;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class Primitives
    {
        [HandleProcessCorruptedStateExceptions]
        public static IMesh GenerateCylinder(IConsole console, IPoint3D location, IVector3D axis, double radius, double height,
            short nLongitude = 30, short nLatitude = -1, short nPlane = -1)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var cylinderResult = new Cylinder()
                    {
                        RadiusX = radius,
                        RadiusY = radius,
                        Height = height,
                        IncludeTop = true,
                        IncludeBottom = true,
                        NLongitude = nLongitude,
                        NLatitude = nLatitude,
                        NPlane = nPlane
                    }.Operate(context);

                    var transformationResult = new TransformationFromPlaneAlignment()
                    {
                        FromOrigin = new Vector3(0.0, 0.0, 0.0),
                        FromNormal = new Vector3(0.0, 0.0, 1.0),
                        ToOrigin = new Vector3(location.X, location.Y, location.Z),
                        ToNormal = new Vector3(axis.X, axis.Y, axis.Z),
                    }.Operate(context);

                    var transformCylinderResult = new Transform()
                    {
                        Vertices = cylinderResult.Vertices,
                        Triangles = cylinderResult.Triangles,
                        Transformation = transformationResult.Transformation
                    }.Operate(context);

                    var vertexArray = (double[,])transformCylinderResult.Vertices.Data;
                    var triangleArray = (ulong[,])transformCylinderResult.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Cylinder", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh GenerateCylinderWithLocationAsBase(IConsole console, IPoint3D location, IVector3D axis, double radius, double height)
        {
            var unitizedAxis = new IDSVector3D(axis);
            unitizedAxis.Unitize();

            var offsetHeight = unitizedAxis.Mul(height / 2);
            var offsetLocation = location.Add(offsetHeight);
            return GenerateCylinder(console, offsetLocation, axis, radius, height);
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh GenerateSphere(IConsole console, IPoint3D location, double radius, long numberOfTriangles = 5120)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var result = new Ellipsoid()
                    {
                        RadiusX = radius,
                        RadiusY = radius,
                        RadiusZ = radius,
                        NumberOfTriangles = numberOfTriangles
                    }.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    var transformedVertexArray = new double[vertexArray.Length, 3];
                    for (var row = 0; row < vertexArray.RowCount(); row++)
                    {
                        var vertex = vertexArray.GetRow(row);
                        transformedVertexArray[row, 0] = vertex[0] + location.X;
                        transformedVertexArray[row, 1] = vertex[1] + location.Y;
                        transformedVertexArray[row, 2] = vertex[2] + location.Z;
                    }

                    return new IDSMesh(transformedVertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Ellipsoid", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        //Width is towards XAxis, Height is towards YAxis and depth is towards ZAxis
        public static IMesh GenerateBox(IConsole console, IPoint3D center, double width, double height, double depth)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var boxResult = new Box()
                    {
                        Width = width,
                        Height = height,
                        Depth = depth,
                        IncludeTop = true,
                        IncludeBottom = true,
                    }.Operate(context);

                    var transformationResult = new TransformationFromPlaneAlignment()
                    {
                        FromOrigin = new Vector3(0.0, 0.0, 0.0),
                        FromNormal = new Vector3(1.0, 1.0, 1.0),
                        ToOrigin = new Vector3(center.X, center.Y, center.Z),
                        ToNormal = new Vector3(1.0, 1.0, 1.0),
                    }.Operate(context);

                    var transformBoxResult = new Transform()
                    {
                        Vertices = boxResult.Vertices,
                        Triangles = boxResult.Triangles,
                        Transformation = transformationResult.Transformation
                    }.Operate(context);

                    var vertexArray = (double[,])transformBoxResult.Vertices.Data;
                    var triangleArray = (ulong[,])transformBoxResult.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Box", e.Message);
                }
            }
        }

        //Created torus is laying in the XZ plane
        [HandleProcessCorruptedStateExceptions]
        public static IMesh GenerateTorus(IConsole console, IPoint3D location, double radiusXScaleFactor, double radiusYScaleFactor, double radius)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var result = new Torus()
                    {
                        RadiusX = radiusXScaleFactor,
                        RadiusY = radiusYScaleFactor,
                        Radius = radius
                    }.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    var transformedVertexArray = new double[vertexArray.Length, 3];
                    for (var row = 0; row < vertexArray.RowCount(); row++)
                    {
                        var vertex = vertexArray.GetRow(row);
                        transformedVertexArray[row, 0] = vertex[0] + location.X;
                        transformedVertexArray[row, 1] = vertex[1] + location.Y;
                        transformedVertexArray[row, 2] = vertex[2] + location.Z;
                    }

                    return new IDSMesh(transformedVertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Torus", e.Message);
                }
            }
        }

        //Created polygon (curve) is located in the XY plane at Z=0
        [HandleProcessCorruptedStateExceptions]
        public static List<ICurve> GeneratePolygon(IConsole console, int numberOfVertices, double radius)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var result = new Polygon()
                    {
                        NumberOfVertices = numberOfVertices,
                        Radius = radius
                    }.Operate(context);

                    var curvePoints = (double[,])result.Vertices.Data;
                    var curveRanges = (long[,])result.Ranges.Data;

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
                catch (Exception e)
                {
                    throw new MtlsException("Polygon", e.Message);
                }
            }
        }
    }
}
