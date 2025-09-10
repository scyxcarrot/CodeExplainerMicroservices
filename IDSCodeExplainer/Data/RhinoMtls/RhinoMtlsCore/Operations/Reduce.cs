using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class Reduce
    {
        /// <summary>
        /// Performs the reduce.
        /// </summary>
        /// <param name="inputMesh">The in mesh.</param>
        /// <param name="iterations">The iterations.</param>
        /// <param name="checkCorners">if set to <c>true</c> [check corners].</param>
        /// <param name="maxValence">The maximum valence.</param>
        /// <param name="minEdgeLength">Minimum length of the edge.</param>
        /// <param name="normalAngle">The normal angle.</param>
        /// <param name="sharpAngle">The sharp angle.</param>
        /// <param name="targetTrianglePercentage">The target triangle percentage.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformReduce(Mesh inputMesh,
            int iterations = 1,
            bool checkCorners = true,
            ulong maxValence = 10,
            double minEdgeLength = 0.0,
            double normalAngle = 5.0,
            double sharpAngle = 30.0,
            double targetTrianglePercentage = 0.5)
        {
            if (iterations <= 0)
            {
                return null;
            }

            var inputMeshTriangles = new Mesh();
            inputMeshTriangles.CopyFrom(inputMesh);
            inputMeshTriangles.Faces.ConvertQuadsToTriangles();

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var op = new MtlsIds34.MeshFix.Reduce()
                {
                    //CheckCorners = checkCorners,
                    //MaxValence = maxValence,
                    //MinEdgeLength = minEdgeLength,
                    //NormalAngle = normalAngle,
                    //NTargetTriangles = (ulong) (inputMeshTriangles.Faces.TriangleCount * targetTrianglePercentage),
                    //SharpAngle = sharpAngle
                    NumberOfIterations = iterations
                };
                op.Triangles = inputMeshTriangles.Faces.ToArray2D(context);
                op.Vertices = inputMeshTriangles.Vertices.ToArray2D(context);

                try
                {
                    var result = op.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Reduce", e.Message);
                }
            }
        }
    }
}