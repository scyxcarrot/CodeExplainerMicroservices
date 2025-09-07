using IDS.Core.V2.Extensions;
using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;
using MtlsIds34.Array;
using System;
using System.Runtime.ExceptionServices;

namespace IDS.Core.V2.MTLS.Operation
{
    public static class RemeshV2
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
        [HandleProcessCorruptedStateExceptions]
        public static IMesh PerformRemesh(IConsole console,
            IMesh inmesh,
            double minEdgeLength = 0.2,
            double maxEdgeLength = 0.7,
            double growthTreshold = 0.2,
            double geometricalError = 0.05,
            double qualityThreshold = 0.4,
            bool preserveSharpEdges = false,
            int iterations = 21)

        {
            var helper = new MtlsIds34ContextHelper(console);
            using (var context = helper.CreateContext())
            {
                var remesher = new MtlsIds34.Remesh.AdaptiveRemesh()
                {
                    MinEdgeLength = minEdgeLength,
                    MaxEdgeLength = maxEdgeLength,
                    GrowthThreshold = growthTreshold,
                    GeometricError = geometricalError,
                    QualityThreshold = qualityThreshold,
                    PreserveSharpEdges = preserveSharpEdges,
                    Iterations = iterations,
                    Triangles = Array2D.Create(context, inmesh.Faces.ToFacesArray2D()),
                    Vertices = Array2D.Create(context, inmesh.Vertices.ToVerticesArray2D()),
                };

                try
                {
                    var result = remesher.Operate(context);

                    var vertexArray = (double[,])result.Vertices.Data;
                    var triangleArray = (ulong[,])result.Triangles.Data;

                    return new IDSMesh(vertexArray, triangleArray);
                }
                catch (Exception e)
                {
                    throw new MtlsException("AdaptiveRemesh", e.Message);
                }
            }
        }
    }
}
