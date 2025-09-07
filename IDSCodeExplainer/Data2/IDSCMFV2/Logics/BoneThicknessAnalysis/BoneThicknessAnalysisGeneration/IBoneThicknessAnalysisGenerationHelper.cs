using IDS.Core.V2.Logic;
using System;

namespace IDS.CMF.V2.Logics
{
    public interface IBoneThicknessAnalysisGenerationHelper : ILogicHelper<BoneThicknessAnalysisGenerationParameters, BoneThicknessAnalysisGenerationResult>
    {
        bool GetBoneThicknessCacheData(Guid objectId, out double[] thicknessData);

        void SetBoneThicknessCacheData(Guid objectId, double[] thicknessData);
    }
}
