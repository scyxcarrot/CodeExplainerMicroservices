using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;

namespace IDS.Core.Utilities
{
    public static class MeshFixingUtilities
    {
        public static bool IsMeshFullyFix(Mesh mesh, bool needSingleShell = true)
        {
            var results = MeshDiagnostics.GetMeshDiagnostics(mesh);

            return IsFullyFix(results, needSingleShell);
        }

        public static bool IsFullyFix(MeshDiagnostics.MeshDiagnosticsResult results, bool needSingleShell = true, bool ignoreOverlapping = false)
        {
            return ((results.NumberOfInvertedNormal == 0) &&
                    (results.NumberOfBadEdges == 0) &&
                    (results.NumberOfBadContours == 0) &&
                    (results.NumberOfNearBadEdges == 0) &&
                    (results.NumberOfHoles == 0) &&
                    ((!needSingleShell) || results.NumberOfShells == 1) &&
                    (ignoreOverlapping || (results.NumberOfOverlappingTriangles == 0)) &&
                    (results.NumberOfIntersectingTriangles == 0));
        }

        public static Mesh PerformComplexFullyFix(Mesh rawMesh, int maxIteration, double sharpWidthThreshold, double sharpAngleThreshold)
        {
            var resultantMesh = PerformComplexRemoveNoiseShell(rawMesh, sharpWidthThreshold, sharpAngleThreshold);
            // TODO: add in fixing inverted normal, bad edges, near bad edges, bad contour in future, currently no case found yet
            resultantMesh = PerformComplexRemoveOverlappingTriangleAndFillHoles(resultantMesh, maxIteration, sharpWidthThreshold, sharpAngleThreshold);
            
            return resultantMesh;
        }

        public static Mesh PerformMinimumFix(Mesh rawMesh, int maxIteration, double sharpWidthThreshold, double sharpAngleThreshold)
        {
            Mesh resultantMesh = null;
            for (int i = 0; i < maxIteration; i++)
            {
                resultantMesh = Triangles.PerformFilterSharpTriangles(rawMesh, sharpWidthThreshold, sharpAngleThreshold);
                resultantMesh = AutoFix.PerformUnify(resultantMesh);
                resultantMesh = AutoFix.RemoveNoiseShells(resultantMesh);
            }
            return resultantMesh;
        }

        [Obsolete("Please use MeshFixingUtilitiesV2.PerformComplexRemoveNoiseShell")]
        public static  Mesh PerformComplexRemoveNoiseShell(Mesh orgMesh, double widthThreshold, double angleThreshold)
        {
            var resultantMesh = AutoFix.RemoveNoiseShells(orgMesh);
            resultantMesh = FilterSharpTriangles(resultantMesh, widthThreshold, angleThreshold);
            return resultantMesh;
        }

        public static Mesh PerformComplexRemoveOverlappingTriangleAndFillHoles(Mesh orgMesh, int maxIteration, double sharpWidthThreshold, double sharpAngleThreshold)
        {
            var resultantMesh = orgMesh.Duplicate() as Mesh;

            for (var i = 0; i < maxIteration; i++)
            {
                var meshDiagnostics = MeshDiagnostics.GetMeshDiagnostics(resultantMesh);
                if (meshDiagnostics.NumberOfOverlappingTriangles <= 0)
                {
                    if (meshDiagnostics.NumberOfIntersectingTriangles <= 0)
                    {
                        break;
                    }
                    else
                    {
                        resultantMesh = FilterSharpTriangles(resultantMesh, sharpWidthThreshold, sharpAngleThreshold);
                        continue;
                    }
                }

                resultantMesh = AutoFix.PerformRemoveOverlappingTriangles(resultantMesh);
                resultantMesh = AutoFix.PerformRemoveIntersectingTriangles(resultantMesh);
                resultantMesh = AutoFix.RemoveNoiseShells(resultantMesh);
                resultantMesh = AutoFix.PerformStitch(resultantMesh);
                resultantMesh = AutoFix.PerformFillHoles(resultantMesh);
                resultantMesh = AutoFix.RemoveNoiseShells(resultantMesh);
            }

            return resultantMesh;
        }

        [Obsolete("Please use MeshFixingUtilitiesV2.FilterSharpTriangles")]
        private static Mesh FilterSharpTriangles(Mesh orgMesh, double widthThreshold, double angleThreshold)
        {
            var resultantMesh = AutoFix.PerformUnify(orgMesh);
            resultantMesh = Triangles.PerformFilterSharpTriangles(resultantMesh, widthThreshold, angleThreshold);
            resultantMesh = AutoFix.PerformUnify(resultantMesh);
            return resultantMesh;
        }
    }
}
