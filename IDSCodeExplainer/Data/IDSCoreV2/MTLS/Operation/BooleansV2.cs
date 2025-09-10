using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using MtlsIds34.MeshDesign;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class BooleansV2
    {
        /// <summary>
        /// Performs the boolean operation.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="mesh1">The mesh1.</param>
        /// <param name="mesh2">The mesh2.</param>
        /// <param name="operation">The operation.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        private static IMesh PerformBooleanOperation(IConsole console, IMesh mesh1, IMesh mesh2, BooleanOperation operation)
        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var boolean = new MtlsIds34.MeshDesign.Boolean()
                {
                    Operation = operation,
                    Algorithm = BooleanAlgorithm.Iu,
                    Triangles1 = Array2D.Create(context, mesh1.Faces.ToFacesArray2D()),
                    Vertices1 = Array2D.Create(context, mesh1.Vertices.ToVerticesArray2D()),
                    Triangles2 = Array2D.Create(context, mesh2.Faces.ToFacesArray2D()),
                    Vertices2 = Array2D.Create(context, mesh2.Vertices.ToVerticesArray2D())
                };

                try
                {
                    var booleanResult = boolean.Operate(context);
                    var vertexArray = (double[,])booleanResult.Vertices.Data;
                    var triangleArray = (ulong[,])booleanResult.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Boolean", e.Message);
                }
            }
        }

        /// <summary>
        /// Performs the boolean union.
        /// </summary>
        /// <param name="console">The console for MTLS.</param>
        /// <param name="unioned">The unioned.</param>
        /// <param name="inmeshes">The inmeshes.</param>
        /// <returns></returns>
        public static bool PerformBooleanUnion(IConsole console, out IMesh unioned, params IMesh[] inmeshes)
        {
            var list = inmeshes.Where(part => !(part == null || part.Vertices.Count == 0)).ToArray();
            unioned = list.First();

            for (var i = 1; i < list.Length; i++)
            {
                unioned = PerformBooleanOperation(console, unioned, list.ElementAt(i), BooleanOperation.Unite);
            }

            return true;
        }

        public static IMesh PerformBooleanIntersection(IConsole console, IMesh mesh1, IMesh mesh2)
        {
            return PerformBooleanOperation(console, mesh1, mesh2, BooleanOperation.Intersect);
        }

        public static IMesh PerformBooleanSubtraction(IConsole console, IMesh meshToBeSubtracted, IMesh sourceMesh)
        {
            return PerformBooleanOperation(console, meshToBeSubtracted, sourceMesh, BooleanOperation.Subtract);
        }
    }
}
