using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Geometry;
using IDS.Interface.Tools;

namespace IDS.CMFImplantCreation.Utilities
{
    public static class MeshFixingUtilities
    {
        public static IMesh PerformComplexFullyFix(IConsole console, IMesh rawMesh, int maxIteration, double sharpWidthThreshold, double sharpAngleThreshold)
        {
            var resultantMesh = PerformComplexRemoveNoiseShell(console, rawMesh, sharpWidthThreshold, sharpAngleThreshold);
            // TODO: add in fixing inverted normal, bad edges, near bad edges, bad contour in future, currently no case found yet
            resultantMesh = PerformComplexRemoveOverlappingTriangleAndFillHoles(console, resultantMesh, maxIteration, sharpWidthThreshold, sharpAngleThreshold);

            return resultantMesh;
        }

        public static IMesh PerformComplexRemoveNoiseShell(IConsole console, IMesh orgMesh, double widthThreshold,
            double angleThreshold)
        {
            var resultantMesh = AutoFixV2.RemoveNoiseShells(console, orgMesh);
            resultantMesh = FilterSharpTriangles(console, resultantMesh, widthThreshold, angleThreshold);
            return resultantMesh;
        }

        public static IMesh PerformComplexRemoveOverlappingTriangleAndFillHoles(IConsole console, IMesh orgMesh, int maxIteration, double sharpWidthThreshold, double sharpAngleThreshold)
        {
            var resultantMesh = orgMesh;

            for (var i = 0; i < maxIteration; i++)
            {
                var meshDiagnostics = MeshDiagnostics.GetMeshDiagnostics(console, resultantMesh);
                if (meshDiagnostics.NumberOfOverlappingTriangles <= 0)
                {
                    if (meshDiagnostics.NumberOfIntersectingTriangles <= 0)
                    {
                        break;
                    }
                    else
                    {
                        resultantMesh = FilterSharpTriangles(console, resultantMesh, sharpWidthThreshold, sharpAngleThreshold);
                        continue;
                    }
                }

                resultantMesh = TrianglesV2.PerformRemoveOverlappingTriangles(console, resultantMesh);
                resultantMesh = TrianglesV2.PerformRemoveIntersectingTriangles(console, resultantMesh);
                resultantMesh = AutoFixV2.RemoveNoiseShells(console, resultantMesh);
                resultantMesh = AutoFixV2.PerformStitch(console, resultantMesh);
                resultantMesh = AutoFixV2.PerformFillHoles(console, resultantMesh);
                resultantMesh = AutoFixV2.RemoveNoiseShells(console, resultantMesh);
            }

            return resultantMesh;
        }

        private static IMesh FilterSharpTriangles(IConsole console, IMesh orgMesh, double widthThreshold, double angleThreshold)
        {
            var resultantMesh = AutoFixV2.PerformUnify(console, orgMesh);
            resultantMesh = TrianglesV2.PerformFilterSharpTriangles(console, resultantMesh, widthThreshold, angleThreshold);
            resultantMesh = AutoFixV2.PerformUnify(console, resultantMesh);
            return resultantMesh;
        }
    }
}
