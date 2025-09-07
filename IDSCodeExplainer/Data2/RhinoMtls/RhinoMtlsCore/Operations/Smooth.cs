using MtlsIds34.Array;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class Smooth
    {
        /// <summary>
        /// Performs the smoothing.
        /// </summary>
        /// <param name="inmesh">The inmesh.</param>
        /// <param name="lambda">The lambda.</param>
        /// <param name="mu">The mu.</param>
        /// <param name="iterations">The interations.</param>
        /// <param name="vertexIndices">The vertex indices.</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformSmoothing(Mesh inmesh,
                                                double lambda = 0.33,
                                                double mu = -0.331,
                                                int iterations = 25,
                                                double[] vertexIndices = null)
        {
            if (lambda < 0 || mu > 0)
            {
                throw new Exception("Lambda must be larger than zero and Mu must be smaller than zero.");
            }

            var input = inmesh.SimplifyMesh();

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var op = new MtlsIds34.MeshFix.Smooth()
                {
                    Lambda = new double[] { lambda },
                    Mu = new double[] { mu },
                    NumberOfIterations = iterations
                };
                op.Triangles = input.Faces.ToArray2D(context);
                op.Vertices = input.Vertices.ToArray2D(context);
                if (vertexIndices != null)
                {
                    op.VertexIndices = Array1D.Create(context, vertexIndices);
                }

                try
                {
                    var result = op.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("Smooth", e.Message);
                }
            }
        }
    }
}
