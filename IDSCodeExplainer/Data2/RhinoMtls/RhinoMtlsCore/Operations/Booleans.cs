using MtlsIds34.MeshDesign;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class Booleans
    {
        /// <summary>
        /// Performs the boolean union.
        /// </summary>
        /// <param name="unioned">The unioned.</param>
        /// <param name="inmeshes">The inmeshes.</param>
        /// <returns></returns>
        [Obsolete("Please use the operation in IDS.Core.V2.MTLS.Operation")]
        public static bool PerformBooleanUnion(out Mesh unioned, params Mesh[] inmeshes)
        {
            var list = inmeshes.Where(part => !(part == null || part.Vertices.Count == 0)).ToArray();
            unioned = list.First();
            
            for (var i = 1; i < list.Length; i++)
            {
                unioned = PerformBooleanOperation(unioned, list.ElementAt(i), BooleanOperation.Unite);
            }

            return true;
        }

        /// <summary>
        /// Performs the boolean operation.
        /// </summary>
        /// <param name="mesh1">The mesh1.</param>
        /// <param name="mesh2">The mesh2.</param>
        /// <param name="operation">The operation.</param>
        /// <returns></returns>
        [Obsolete("Please use the operation in IDS.Core.V2.MTLS.Operation")]
        [HandleProcessCorruptedStateExceptions]
        private static Mesh PerformBooleanOperation(Mesh mesh1, Mesh mesh2, BooleanOperation operation)
        {
            if (mesh1.Faces.QuadCount > 0)
            {
                mesh1.Faces.ConvertQuadsToTriangles();
            }

            if (mesh2.Faces.QuadCount > 0)
            {
                mesh2.Faces.ConvertQuadsToTriangles();
            }

            using (var context = MtlsIds34Globals.CreateContext())
            {

                var boolean = new MtlsIds34.MeshDesign.Boolean()
                {
                    Operation = operation,
                    Algorithm = BooleanAlgorithm.Iu
                };

                boolean.Triangles1 = mesh1.Faces.ToArray2D(context);
                boolean.Vertices1 = mesh1.Vertices.ToArray2D(context);
                boolean.Triangles2 = mesh2.Faces.ToArray2D(context);
                boolean.Vertices2 = mesh2.Vertices.ToArray2D(context);

                try
                {
                    var booleanResult = boolean.Operate(context);
                    var vertexArray = booleanResult.Vertices.ToDouble2DArray();
                    var triangleArray = booleanResult.Triangles.ToUint64Array();

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Boolean", e.Message);
                }
            }
        }

        /// <summary>
        /// Performs the boolean intersection.
        /// </summary>
        /// <param name="mesh1">The mesh1.</param>
        /// <param name="mesh2">The mesh2.</param>
        /// <returns></returns>
        public static Mesh PerformBooleanIntersection(Mesh mesh1, Mesh mesh2)
        {
            return PerformBooleanOperation(mesh1, mesh2, BooleanOperation.Intersect);
        }

        /// <summary>
        /// Performs the boolean subtraction.
        /// </summary>
        /// <param name="meshesToBeSubtracted">The meshes to be subtracted.</param>
        /// <param name="sourceMesh">The source mesh.</param>
        /// <returns></returns>
        public static Mesh PerformBooleanSubtraction(List<Mesh> meshesToBeSubtracted, Mesh sourceMesh)
        {
            var meshToBeSubtracted = new Mesh();
            foreach (var mesh in meshesToBeSubtracted)
            {
                meshToBeSubtracted.Append(mesh);
            }
            return PerformBooleanSubtraction(meshToBeSubtracted, sourceMesh);
        }

        /// <summary>
        /// Performs the boolean subtraction.
        /// </summary>
        /// <param name="meshToBeSubtracted">The mesh to be subtracted.</param>
        /// <param name="sourceMeshes">The source meshes.</param>
        /// <returns></returns>
        public static Mesh PerformBooleanSubtraction(Mesh meshToBeSubtracted, List<Mesh> sourceMeshes)
        {
            var sourceMesh = sourceMeshes.First();
            for (var i = 1; i < sourceMeshes.Count; i++)
            {
                sourceMesh.Append(sourceMeshes.ElementAt(i));
            }
            return PerformBooleanSubtraction(meshToBeSubtracted, sourceMesh);
        }

        /// <summary>
        /// Performs the boolean subtraction.
        /// </summary>
        /// <param name="meshToBeSubtracted">The mesh to be subtracted.</param>
        /// <param name="sourceMesh">The source mesh.</param>
        /// <returns></returns>
        public static Mesh PerformBooleanSubtraction(Mesh meshToBeSubtracted, Mesh sourceMesh)
        {
            return PerformBooleanOperation(meshToBeSubtracted, sourceMesh, BooleanOperation.Subtract);
        }
    }
}