using MtlsIds34.Array;
using MtlsIds34.Core;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class TrimIntoHalves
    {
        public Mesh PerformTrimToGetInnerMesh(Mesh inmesh, List<Point3d> pointsOnPlane, Plane projectionPlane)
        {
            Mesh innerMesh;
            Mesh outerMesh;

            PerformTrimIntoHalves(inmesh, pointsOnPlane, projectionPlane, out innerMesh, out outerMesh);

            return innerMesh;
        }

        public Mesh PerformTrimToGetOuterMesh(Mesh inmesh, List<Point3d> pointsOnPlane, Plane projectionPlane)
        {
            Mesh innerMesh;
            Mesh outerMesh;

            PerformTrimIntoHalves(inmesh, pointsOnPlane, projectionPlane, out innerMesh, out outerMesh);

            return outerMesh;
        }

        [HandleProcessCorruptedStateExceptions]
        private void PerformTrimIntoHalves(Mesh inmesh, List<Point3d> pointsOnPlane, Plane projectionPlane, out Mesh innerMesh, out Mesh outerMesh)
        {
            var worldPlane = Plane.WorldXY;
            var localPlane = projectionPlane;
            var screenPlane = new Plane(new Point3d(), localPlane.Normal);

            var translate = Transform.Translation(new Point3d() - localPlane.Origin);
            var transformScreenToWorld = Transform.PlaneToPlane(screenPlane, worldPlane);
            var transformPlaneToWorld = Transform.Multiply(transformScreenToWorld, translate);

            Transform transformWorldToPlane;
            if (!transformPlaneToWorld.TryGetInverse(out transformWorldToPlane))
            {
                throw new Exception("TrimIntoHalves: Unable to inverse transformation!");
            }

            var duplicatedMesh = inmesh.DuplicateMesh();

            if (duplicatedMesh.Faces.QuadCount > 0)
            {
                duplicatedMesh.Faces.ConvertQuadsToTriangles();
            }

            var pointsIn2D = new List<Point3d>();
            foreach (var point in pointsOnPlane)
            {
                var localPoint = point;
                localPoint.Transform(transformPlaneToWorld);
                pointsIn2D.Add(localPoint);
            }

            duplicatedMesh.Transform(transformPlaneToWorld);

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var trimer = new MtlsIds34.MeshFinish.TrimIntoHalves();
                trimer.Triangles = duplicatedMesh.Faces.ToArray2D(context);
                trimer.Vertices = duplicatedMesh.Vertices.ToArray2D(context);
                trimer.TrimmingContourPoints = ToPoint2D(pointsIn2D, context);
                trimer.TrimmingContourTransformation = ToTransformationMatrix(Transform.Identity);

                try
                {
                    var result = trimer.Operate(context);

                    var insideVertexArray = result.InsideVertices.ToDouble2DArray();
                    var insideTriangleArray = result.InsideTriangles.ToUint64Array();
                    var outsideVertexArray = result.OutsideVertices .ToDouble2DArray();
                    var outsideTriangleArray = result.OutsideTriangles.ToUint64Array();

                    innerMesh = MeshUtilities.MakeRhinoMesh(insideVertexArray, insideTriangleArray);
                    outerMesh = MeshUtilities.MakeRhinoMesh(outsideVertexArray, outsideTriangleArray);

                    innerMesh.Transform(transformWorldToPlane);
                    outerMesh.Transform(transformWorldToPlane);
                }
                catch (Exception e)
                {
                    throw new MtlsException("TrimIntoHalves", e.Message);
                }
            }
        }

        private Array2D ToPoint2D(List<Point3d> points, Context context)
        {
            const int coordinatesPerVertex = 2;
            var doubleArray = new double[points.Count, coordinatesPerVertex];

            for (var i = 0; i < points.Count; i++)
            {
                doubleArray[i, 0] = points[i].X;
                doubleArray[i, 1] = points[i].Y;
                //strip out Z
            }

            return Array2D.Create(context, doubleArray);
        }

        private MtlsIds34.Core.Primitives.TransformationMatrix ToTransformationMatrix(Transform trans)
        {
            var transform = new MtlsIds34.Core.Primitives.TransformationMatrix
                (
                    trans.M00,
                    trans.M01,
                    trans.M02,
                    trans.M03,

                    trans.M10,
                    trans.M11,
                    trans.M12,
                    trans.M13,

                    trans.M20,
                    trans.M21,
                    trans.M22,
                    trans.M23,

                    trans.M30,
                    trans.M31,
                    trans.M32,
                    trans.M33
                );

            return transform;
        }
    }
}
