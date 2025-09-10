using IDS.Core.V2.Logic;
using IDS.Interface.Logic;
using IDS.Interface.Tools;

namespace IDS.CMF.V2.Logics
{
    public class BoneThicknessAnalysisIntegrateLogic: Logic<BoneThicknessAnalysisIntegrateParameters, BoneThicknessAnalysisIntegrateResult>
    {
        private readonly IBoneThicknessAnalysisIntegrateHelper _specificLogicHelper;

        public BoneThicknessAnalysisIntegrateLogic(IConsole console,
            IBoneThicknessAnalysisIntegrateHelper logicHelper) : base(console, logicHelper)
        {
            _specificLogicHelper = logicHelper;
        }

        private void LogWallThicknessDoOperation(bool complete)
        {
            var additionalString = complete ? "COMPLETE" : "";
            console.WriteLine($"Performing Minimum Wall Thickness = {_specificLogicHelper.CurrentMinWallThickness} and Maximum Wall Thickness = {_specificLogicHelper.CurrentMaxWallThickness}...{additionalString}");
        }

        protected override LogicStatus OnExecute(BoneThicknessAnalysisIntegrateParameters parameters, out BoneThicknessAnalysisIntegrateResult result)
        {
            LogWallThicknessDoOperation(false);
            result = new BoneThicknessAnalysisIntegrateResult();

            var boneThicknessAnalysisGenerationHelper = _specificLogicHelper.GetBoneAnalysisGenerationHelper();
            var boneThicknessAnalysisGenerationLogic = new BoneThicknessAnalysisGenerationLogic(console, boneThicknessAnalysisGenerationHelper);
            var status = boneThicknessAnalysisGenerationLogic.Execute(out var boneThicknessAnalysisGenerationResult);
            if (status != LogicStatus.Success)
            {
                return status;
            }

            var boneThicknessAnalysisReportingHelper = _specificLogicHelper.GetBoneThicknessAnalysisReportingHelper(boneThicknessAnalysisGenerationResult.ThicknessData);
            var boneThicknessAnalysisLogic = new BoneThicknessAnalysisReportingLogic(console, boneThicknessAnalysisReportingHelper);
            status = boneThicknessAnalysisLogic.Execute(out _);

            if (status == LogicStatus.Success)
            {
                LogWallThicknessDoOperation(true);
            }
            
            return status;
        }
    }
}
