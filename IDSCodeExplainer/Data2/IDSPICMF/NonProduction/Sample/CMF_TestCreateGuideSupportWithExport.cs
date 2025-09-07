using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.PICMF.Commands;
using IDS.PICMF.Visualization;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.UI;
using RhinoMtlsCore.Operations;
using Style = Rhino.Commands.Style;

namespace IDS.PICMF.NonProduction
{
#if (INTERNAL)
    [System.Runtime.InteropServices.Guid("2C86F9B0-385D-4ED6-8AC5-757ED6F23C84")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideSupportRoI)]
    public class CMF_TestCreateGuideSupportWithExport : CMFCreateGuideSupport
    {
        public CMF_TestCreateGuideSupportWithExport()
        {
            TheCommand = this;
            VisualizationComponent = new CMFCreateGuideSupportVisualizationComponent();
        }

        public new static CMF_TestCreateGuideSupportWithExport TheCommand { get; private set; }

        public override string EnglishName => "CMF_TestCreateGuideSupportWithExport";

        // Override the function to 100% export the guide support
        protected override bool ValidateQualityOfSupport(CMFImplantDirector director, MeshDiagnostics.MeshDiagnosticsResult results, Mesh rawSupport, Mesh notFullyFixedSupport)
        {
            var analysisResult = PostSupportCreationHelper.GetAnalysisResult(results);
            ExportForUserAnalysis(director.Document, rawSupport, notFullyFixedSupport);

            if (analysisResult == PostSupportCreationHelper.AnalysisResult.BadTriangle)
            {
                return false;
            }

            if (analysisResult == PostSupportCreationHelper.AnalysisResult.OverlappingTriangleOnly)
            {
                var proceedSupportReplacement =
                    GetConfirmationToProceedWithOverlappingTriangle(director, notFullyFixedSupport);

                if (proceedSupportReplacement == ShowMessageResult.No)
                {
                    return false;
                }
            }
            return true;
        }
    }
#endif
}
