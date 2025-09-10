using MtlsIds34.Array;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class RayIntersection
    {
        /// <summary>
        /// Performs the ray intersection.
        /// </summary>
        /// <param name="meshes">The meshes.</param>
        /// <param name="origin">The origin.</param>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public static Point3d[] PerformRayIntersection(Mesh[] meshes, Point3d origin, Vector3d direction)
        {
            return PerformRayIntersection(meshes, new[] { new RayData(origin, direction) });
        }

        /// <summary>
        /// Performs the ray intersection.
        /// </summary>
        /// <param name="meshes">The meshes.</param>
        /// <param name="rayData">The ray data.</param>
        /// <returns></returns>
        public static Point3d[] PerformRayIntersection(Mesh[] meshes, RayData rayData)
        {
            return PerformRayIntersection(meshes, new[] { rayData });
        }

        /// <summary>
        /// Performs the ray intersection.
        /// </summary>
        /// <param name="meshes">The meshes.</param>
        /// <param name="rayDatas">The ray datas.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static Point3d[] PerformRayIntersection(Mesh[] meshes, RayData[] rayDatas)
        {
            var targetMesh = MeshUtilities.MergeMeshes(meshes);

            if (null == targetMesh)
            {
                return null;
            }

            if (targetMesh.Faces.QuadCount > 0)
            {
                targetMesh.Faces.ConvertQuadsToTriangles();
            }

            var nRays = rayDatas.Length;
            var origins = new double[nRays, 3];
            var directions = new double[nRays, 3];

            for (var i = 0; i < nRays; ++i)
            {
                var currOrigin = rayDatas[i].Origin.ToDouble2DArray();
                origins[i, 0] = currOrigin[0, 0];
                origins[i, 1] = currOrigin[0, 1];
                origins[i, 2] = currOrigin[0, 2];

                var currDirection = rayDatas[i].Direction.ToDouble2DArray();
                directions[i, 0] = currDirection[0, 0];
                directions[i, 1] = currDirection[0, 1];
                directions[i, 2] = currDirection[0, 2];
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var op = new MtlsIds34.Geometry.IntersectionsMeshAndRays();
                op.Directions = Array2D.Create(context, directions);
                op.Origins = Array2D.Create(context, origins);
                op.Triangles = targetMesh.Faces.ToArray2D(context);
                op.Vertices = targetMesh.Vertices.ToArray2D(context);

                try
                {
                    var result = op.Operate(context);

                    if (!result.CutPoints.IsValid)
                    {
                        return null;
                    }

                    var points = (double[,])result.CutPoints.Data;

                    int nRows = points.Length / 3;

                    var intersectionPts = new Point3d[nRows];

                    for (var i = 0; i < nRows; ++i)
                    {
                        intersectionPts[i] = new Point3d(points[i, 0], points[i, 1], points[i, 2]);
                    }

                    return intersectionPts;
                }
                catch (Exception e)
                {
                    throw new MtlsException("RaysIntersections", e.Message);
                }
            }
        }
    }
}
