using Rhino;
using Rhino.Geometry;

namespace RhinoMtlsCore.Operations
{
    public class SmoothImplant
    {
        public static bool OperatorSmoothEdge(Mesh top, Mesh side, Mesh bottom, double topInfluenceDistance, double bottomInfluenceDistance, double topMinEdgeLength, double topMaxEdgeLength, double bottomMinEdgeLength, double bottomMaxEdgeLength, uint iterations, out Mesh rounded)
        {
            var topCurve = GetMeshContour(top);
            var bottomCurve = GetMeshContour(bottom);
            
            var implant = new Mesh();
            implant.Append(top);
            implant.Append(side);
            implant.Append(bottom);

            implant = AutoFix.PerformUnify(implant);

            var smoothedTop = SmoothEdge.PerformEdgeSmoothing(implant, topCurve, out rounded,
                                                                            topInfluenceDistance,
                                                                            iterations,
                                                                            true,
                                                                            topMaxEdgeLength,
                                                                            topMinEdgeLength);

            if (smoothedTop)
            {
                var smoothedBottom = SmoothEdge.PerformEdgeSmoothing(rounded, bottomCurve, out rounded,
                                                                                bottomInfluenceDistance,
                                                                                iterations,
                                                                                true,
                                                                                bottomMaxEdgeLength,
                                                                                bottomMinEdgeLength);
                if (smoothedBottom)
                {
                    return true;
                }
                RhinoApp.WriteLine("[MTLS:Error] Could not round implant bottom.");
                return false;
            }
            RhinoApp.WriteLine("[MTLS:Error] Could not round implant top.");
            return false;
        }

        private static Polyline GetMeshContour(Mesh mesh)
        {
            var nakedEdges = mesh.GetNakedEdges()[0];
            return nakedEdges;
        }
    }
}