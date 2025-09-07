using IDS.Core.Utilities;
using RhinoMtlsCore.Operations;

namespace IDS.CMF.Utilities
{

    public class PostSupportCreationHelper
    {
        public enum AnalysisResult
        {
            BadTriangle,
            OverlappingTriangleOnly,
            CompletelyOk,
            Unknown
        }

        public static AnalysisResult GetAnalysisResult(MeshDiagnostics.MeshDiagnosticsResult results)
        {
            if (results == null)
            {
                return AnalysisResult.Unknown;
            }

            if (!MeshFixingUtilities.IsFullyFix(results, false, true))
            {
                return AnalysisResult.BadTriangle;
            }

            return (results.NumberOfOverlappingTriangles > 0)
                ? AnalysisResult.OverlappingTriangleOnly
                : AnalysisResult.CompletelyOk;
        }
    }
}
