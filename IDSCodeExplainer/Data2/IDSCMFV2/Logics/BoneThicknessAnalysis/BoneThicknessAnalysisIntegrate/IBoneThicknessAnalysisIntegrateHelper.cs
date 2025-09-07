using IDS.Core.V2.Logic;

namespace IDS.CMF.V2.Logics
{
    public interface IBoneThicknessAnalysisIntegrateHelper : 
        ILogicHelper<BoneThicknessAnalysisIntegrateParameters, BoneThicknessAnalysisIntegrateResult>
    {
        double CurrentMinWallThickness { get; }

        double CurrentMaxWallThickness { get; }

        IBoneThicknessAnalysisGenerationHelper GetBoneAnalysisGenerationHelper();

        IBoneThicknessAnalysisReportingHelper GetBoneThicknessAnalysisReportingHelper(double[] thicknessData);
    }
}
