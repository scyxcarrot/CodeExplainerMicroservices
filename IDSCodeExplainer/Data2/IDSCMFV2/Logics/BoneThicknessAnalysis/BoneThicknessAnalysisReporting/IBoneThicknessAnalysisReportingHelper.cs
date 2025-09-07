using IDS.Core.V2.Logic;
using System;

namespace IDS.CMF.V2.Logics
{
    public interface IBoneThicknessAnalysisReportingHelper : ILogicHelper<BoneThicknessAnalysisReportingParameters, BoneThicknessAnalysisReportingResult>
    {
        bool GetBuildingBlockThicknessMinMax(Guid objectId, ref double minWallThickness, ref double maxWallThickness);

        void SetBuildingBlockThicknessMinMax(Guid objectId, double minWallThickness, double maxWallThickness);
    }
}
