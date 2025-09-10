using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.Core.Primitives;
using MtlsIds34.Geometry;
using MtlsIds34.Math;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class GeometryTransformation
    {
        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformMeshTransform(
            IConsole console,
            IMesh mesh,
            IVector3D translationUnitVector,
            double distance)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    translationUnitVector.Unitize();
                    var translationVector = translationUnitVector.Mul(distance);
                    // code formatted like this to make it easy to see the matrix
                    var transformationMatrixMtls = new TransformationMatrix(
                        1.0, 0.0, 0.0, translationVector.X,
                        0.0, 1.0, 0.0, translationVector.Y,
                        0.0, 0.0, 1.0, translationVector.Z,
                        0.0, 0.0, 0.0, 1.0
                        );

                    var transformMeshResult = new MtlsIds34.MeshDesign.Transform()
                    {
                        Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                        Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                        Transformation = transformationMatrixMtls
                    }.Operate(context);

                    var vertexArray = (double[,])transformMeshResult.Vertices.Data;
                    var triangleArray = (ulong[,])transformMeshResult.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Transform", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformMeshTransform(IConsole console, IMesh mesh, IPoint3D fromOrigin, IVector3D fromNormal,
            IPoint3D toOrigin, IVector3D toNormal)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var transformationResult = new TransformationFromPlaneAlignment()
                    {
                        FromOrigin = new Vector3(fromOrigin.X, fromOrigin.Y, fromOrigin.Z),
                        FromNormal = new Vector3(fromNormal.X, fromNormal.Y, fromNormal.Z),
                        ToOrigin = new Vector3(toOrigin.X, toOrigin.Y, toOrigin.Z),
                        ToNormal = new Vector3(toNormal.X, toNormal.Y, toNormal.Z),
                    }.Operate(context);

                    var transformMeshResult = new MtlsIds34.MeshDesign.Transform()
                    {
                        Triangles = Array2D.Create(context, mesh.Faces.ToFacesArray2D()),
                        Vertices = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                        Transformation = transformationResult.Transformation
                    }.Operate(context);

                    var vertexArray = (double[,])transformMeshResult.Vertices.Data;
                    var triangleArray = (ulong[,])transformMeshResult.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Transform", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformMeshTransformOperation(IConsole console, IMesh mesh, ITransform transformationMatrix)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var mtlsTransformationMatrix = GetMtlsTransformationMatrix(transformationMatrix);
                var transformedMesh = new Transform()
                {
                    Coordinates = Array2D.Create(context, mesh.Vertices.ToVerticesArray2D()),
                    Transformation = mtlsTransformationMatrix
                };

                try
                {
                    var transformedResult = transformedMesh.Operate(context);
                    var transformedVertices = (double[,])transformedResult.TransformedCoordinates.Data;

                    return new IDSMesh(transformedVertices, mesh.Faces.ToFacesArray2D());
                }
                catch (Exception e)
                {
                    throw new MtlsException("PerformMeshTransformOperation", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IVector3D PerformVectorTransformOperation(IConsole console, IVector3D vector, ITransform transformationMatrix)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var mtlsTransformationMatrix = GetMtlsTransformationMatrix(transformationMatrix);
                var transformedMesh = new TransformVector()
                {
                    Vector = new Vector3(vector.X, vector.Y, vector.Z),
                    Transformation = mtlsTransformationMatrix
                };

                try
                {
                    var transformedResult = transformedMesh.Operate(context);
                    var transformedVector = transformedResult.Vector;

                    return new IDSVector3D(transformedVector.x, transformedVector.y, transformedVector.z);
                }
                catch (Exception e)
                {
                    throw new MtlsException("PerformVectorTransformOperation", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IPoint3D PerformPointTransformOperation(IConsole console, IPoint3D point, ITransform transformationMatrix)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var mtlsTransformationMatrix = GetMtlsTransformationMatrix(transformationMatrix);
                var transformedMesh = new TransformPoint()
                {
                    Point = new Vector3(point.X, point.Y, point.Z),
                    Transformation = mtlsTransformationMatrix
                };

                try
                {
                    var transformedResult = transformedMesh.Operate(context);
                    var transformedPoint = transformedResult.Point;

                    return new IDSPoint3D(transformedPoint.x, transformedPoint.y, transformedPoint.z);
                }
                catch (Exception e)
                {
                    throw new MtlsException("PerformPointTransformOperation", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static ITransform GetTransformationFromPlaneToPlane(IConsole console, IPlane fromPlane, IPlane toPlane)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var planeToPlaneTransformation = new GetTransformationFromPlaneAlignment()
                {
                    FromOrigin = new Vector3(fromPlane.Origin.X, fromPlane.Origin.Y, fromPlane.Origin.Z),
                    FromNormal = new Vector3(fromPlane.Normal.X, fromPlane.Normal.Y, fromPlane.Normal.Z),
                    ToOrigin = new Vector3(toPlane.Origin.X, toPlane.Origin.Y, toPlane.Origin.Z),
                    ToNormal = new Vector3(toPlane.Normal.X, toPlane.Normal.Y, toPlane.Normal.Z),
                };

                try
                {
                    var planeToPlaneTransformationResult = planeToPlaneTransformation.Operate(context);
                    var mtlsTransformationMatrix = planeToPlaneTransformationResult.Transformation;
                    var idsTransformationMatrix = GetIdsTransformationMatrix(mtlsTransformationMatrix);
                    
                    return idsTransformationMatrix;
                }
                catch (Exception e)
                {
                    throw new MtlsException("GetTransformationFromPlaneToPlane", e.Message);
                }
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformMeshScalingOperation(IConsole console, IMesh mesh, double scaleFactor)
        {
            var meshDimensions = MeshDiagnostics.GetMeshDimensions(console, mesh);
            var centerOfGravity = new IDSPoint3D(meshDimensions.CenterOfGravity);

            ITransform transformationMatrix = null;

            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                try
                {
                    var result = new TransformationFromScaling()
                    {
                        ScaleX = scaleFactor,
                        ScaleY = scaleFactor,
                        ScaleZ = scaleFactor,
                        Center = new Vector3(centerOfGravity.X, centerOfGravity.Y, centerOfGravity.Z)
                    }.Operate(context);

                    transformationMatrix = GetIdsTransformationMatrix(result.Transformation);
                }
                catch (Exception e)
                {
                    throw new MtlsException("PerformMeshScalingOperation", e.Message);
                }
            }

            return PerformMeshTransformOperation(console, mesh, transformationMatrix);
        }

        internal static ITransform GetIdsTransformationMatrix(TransformationMatrix mtlsTransformationMatrix)
        {
            var idsTransformationMatrix = new IDSTransform()
            {
                M00 = mtlsTransformationMatrix.a11,
                M01 = mtlsTransformationMatrix.a12,
                M02 = mtlsTransformationMatrix.a13,
                M03 = mtlsTransformationMatrix.a14,
                M10 = mtlsTransformationMatrix.a21,
                M11 = mtlsTransformationMatrix.a22,
                M12 = mtlsTransformationMatrix.a23,
                M13 = mtlsTransformationMatrix.a24,
                M20 = mtlsTransformationMatrix.a31,
                M21 = mtlsTransformationMatrix.a32,
                M22 = mtlsTransformationMatrix.a33,
                M23 = mtlsTransformationMatrix.a34,
                M30 = mtlsTransformationMatrix.a41,
                M31 = mtlsTransformationMatrix.a42,
                M32 = mtlsTransformationMatrix.a43,
                M33 = mtlsTransformationMatrix.a44
            };

            return idsTransformationMatrix;
        }

        private static TransformationMatrix GetMtlsTransformationMatrix(ITransform idsTransformationMatrix)
        {
            var mtlsTransformationMatrix = new TransformationMatrix(
                idsTransformationMatrix.M00,
                idsTransformationMatrix.M01,
                idsTransformationMatrix.M02,
                idsTransformationMatrix.M03,
                idsTransformationMatrix.M10,
                idsTransformationMatrix.M11,
                idsTransformationMatrix.M12,
                idsTransformationMatrix.M13,
                idsTransformationMatrix.M20,
                idsTransformationMatrix.M21,
                idsTransformationMatrix.M22,
                idsTransformationMatrix.M23,
                idsTransformationMatrix.M30,
                idsTransformationMatrix.M31,
                idsTransformationMatrix.M32,
                idsTransformationMatrix.M33
            );

            return mtlsTransformationMatrix;
        }
    }
}
