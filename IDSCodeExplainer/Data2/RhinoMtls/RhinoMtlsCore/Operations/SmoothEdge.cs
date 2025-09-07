using Rhino;
using Rhino.Geometry;
using RhinoMtlsCore.Common;
using RhinoMtlsCore.Utilities;
using System;
using System.Runtime.ExceptionServices;

namespace RhinoMtlsCore.Operations
{
    public class SmoothEdge
    {
        public static bool PerformEdgeSmoothing(Mesh inputMesh,
            Polyline edge,
            out Mesh rounded,
            double regionOfInfluence = 2,
            uint iterations = 10,
            bool autoSubdivide = false,
            double maxEdgeLength = 0.7,
            double minEdgeLength = 0.0001,
            double badThreshold = 0.4,
            bool fastCollapse = true,
            bool flipEdges = true,
            bool ignoreSurfaceInfo = false,
            bool remeshLowQuality = false,
            bool skipBorder = false,
            SmoothSubdivisionMethod subdivisionMethod = SmoothSubdivisionMethod.Linear)
        {
            var edgePoints = edge.ToDouble2DArray();
            var success = PerformEdgeSmoothing(inputMesh, edgePoints, out rounded, regionOfInfluence, iterations,
                autoSubdivide, maxEdgeLength, minEdgeLength, badThreshold, fastCollapse, flipEdges, ignoreSurfaceInfo,
                remeshLowQuality, skipBorder, subdivisionMethod);

            return success;
        }

        public static bool PerformEdgeSmoothing(Mesh inputMesh,
            Curve edge,
            out Mesh rounded,
            double regionOfInfluence = 2,
            uint iterations = 10,
            bool autoSubdivide = false,
            double maxEdgeLength = 0.7,
            double minEdgeLength = 0.0001,
            double badThreshold = 0.4,
            bool fastCollapse = true,
            bool flipEdges = true,
            bool ignoreSurfaceInfo = false,
            bool remeshLowQuality = false,
            bool skipBorder = false,
            SmoothSubdivisionMethod subdivisionMethod = SmoothSubdivisionMethod.Linear)
        {
            var edgePoints = inputMesh.GetEdgePoints(edge);
            var success = PerformEdgeSmoothing(inputMesh, edgePoints, out rounded, regionOfInfluence, iterations,
                autoSubdivide, maxEdgeLength, minEdgeLength, badThreshold, fastCollapse, flipEdges, ignoreSurfaceInfo,
                remeshLowQuality, skipBorder, subdivisionMethod);

            return success;
        }

        [HandleProcessCorruptedStateExceptions]
        private static bool PerformEdgeSmoothing(Mesh inputMesh,
                                                double[,] edge,
                                                out Mesh rounded,
                                                double regionOfInfluence,
                                                uint iterations, 
                                                bool autoSubdivide,
                                                double maxEdgeLength, 
                                                double minEdgeLength, 
                                                double badThreshold, 
                                                bool fastCollapse, 
                                                bool flipEdges, 
                                                bool ignoreSurfaceInfo, 
                                                bool remeshLowQuality,
                                                bool skipBorder, 
                                                SmoothSubdivisionMethod subdivisionMethod)
        {
            bool success;
            rounded = null;

            if (edge.GetLength(0) > 0)
            { 
                var smoothEdge = new Mtls.Imdck.SmoothEdge(MtlsGlobals.MtlsContext);
                smoothEdge.Triangles.FromArray(inputMesh.Faces.ToUint64Array());
                smoothEdge.Vertices.FromArray(inputMesh.Vertices.ToDouble2DArray());
                smoothEdge.EdgePoints.FromArray(edge);
                smoothEdge.RegionOfInfluence = regionOfInfluence;
                smoothEdge.Iterations = iterations;
                smoothEdge.AutoSubdivide = autoSubdivide;
                smoothEdge.RemeshLowQuality = remeshLowQuality;
                smoothEdge.SkipBorder = skipBorder;
                smoothEdge.IgnoreSurfaceInfo = ignoreSurfaceInfo;
                smoothEdge.MaxEdgeLength = maxEdgeLength;
                smoothEdge.MinEdgeLength = minEdgeLength;
                smoothEdge.BadThreshold = badThreshold;
                smoothEdge.FastCollapse = fastCollapse;
                smoothEdge.FlipEdges = flipEdges;
                smoothEdge.SubdivisionMethod = subdivisionMethod.Convert();

                try
                {
                    var result = smoothEdge.Operate();
                    var vertexArray = result.Vertices.Data.ToDoubleArray();
                    var triangleArray = result.Triangles.Data.ToUInt64Array();

                    result.Dispose();
                    rounded = MeshUtilities.MakeRhinoMesh(vertexArray, triangleArray);
                    success = true;
                }
                catch (Exception e)
                {
                    RhinoApp.WriteLine("[MTLS:Error] Failed to smooth");
                    RhinoApp.WriteLine(e.Message);
                    success = false;
                }
            }
            else
            {
                success = false;
            }

            return success;
        }
    }
}