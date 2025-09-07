using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class Remesh
    {
        /// <summary>
        /// Performs the remesh.
        /// </summary>
        /// <param name="inmesh">The inmesh.</param>
        /// <param name="minEdgeLength">Minimum length of the edge.</param>
        /// <param name="checkMaximumEdgeLength">if set to <c>true</c> [check maximum edge length].</param>
        /// <param name="maxEdgeLength">Maximum length of the edge.</param>
        /// <param name="checkGrowth">if set to <c>true</c> [check growth].</param>
        /// <param name="growthTreshold">The growth treshold.</param>
        /// <param name="geometricalError">The geometrical error.</param>
        /// <param name="qualityThreshold">The quality threshold.</param>
        /// <param name="preserveSharpEdges">if set to <c>true</c> [preserve sharp edges].</param>
        /// <param name="iterations">The iterations.</param>
        /// <returns></returns>
        [Obsolete("Please use the operation in IDS.Core.V2.MTLS.Operation")]
        [HandleProcessCorruptedStateExceptions]
        public static Mesh PerformRemesh(Mesh inmesh,
                                            double minEdgeLength = 0.2,
                                            double maxEdgeLength = 0.7,
                                            double growthTreshold = 0.2,
                                            double geometricalError = 0.05,
                                            double qualityThreshold = 0.4,
                                            bool preserveSharpEdges = false,
                                            int iterations = 21)

        {
            if (inmesh.Faces.QuadCount > 0)
                inmesh.Faces.ConvertQuadsToTriangles();

            using (var context = MtlsIds34Globals.CreateContext())
            {
                var remesher = new MtlsIds34.Remesh.AdaptiveRemesh()
                {
                    MinEdgeLength = minEdgeLength,
                    MaxEdgeLength = maxEdgeLength,
                    GrowthThreshold = growthTreshold,
                    GeometricError = geometricalError,
                    QualityThreshold = qualityThreshold,
                    PreserveSharpEdges = preserveSharpEdges,
                    Iterations = iterations
                };
                remesher.Triangles = inmesh.Faces.ToArray2D(context);
                remesher.Vertices = inmesh.Vertices.ToArray2D(context);

                try
                {
                    var result = remesher.Operate(context);

                    var vertexArray = result.Vertices.ToDouble2DArray();
                    var triangleArray = result.Triangles.ToUint64Array();

                    return MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("AdaptiveRemesh", e.Message);
                }
            }
        }

        /// <summary>
        /// Performs the remesh.
        /// </summary>
        /// <param name="inmeshes">The inmeshes.</param>
        /// <param name="maxEdgeLength">Maximum length of the edge.</param>
        /// <param name="qualityThreshold">The quality threshold.</param>
        /// <returns></returns>
        public static Mesh[] PerformRemesh(Mesh[] inmeshes, double maxEdgeLength, double qualityThreshold)
        {
            int size = inmeshes.Length;
            Mesh[] results = new Mesh[size];

            for (int i = 0; i < size; ++i)
            {
                var remeshed = PerformRemesh(inmeshes[i], maxEdgeLength: maxEdgeLength, qualityThreshold: qualityThreshold);

                if(null != remeshed)
                {
                    results[i] = remeshed;
                }
            }

            return results;
        }
    }
}
