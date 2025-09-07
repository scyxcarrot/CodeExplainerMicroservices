using IDS.Core.V2.Logic;
using IDS.Core.V2.MTLS.Operation;
using IDS.Interface.Logic;
using IDS.Interface.Tools;

namespace IDS.CMF.V2.Logics
{
    public class BoneThicknessAnalysisGenerationLogic : 
        Logic<BoneThicknessAnalysisGenerationParameters, BoneThicknessAnalysisGenerationResult>
    {
        private readonly IBoneThicknessAnalysisGenerationHelper _specificLogicHelper;

        public BoneThicknessAnalysisGenerationLogic(IConsole console,
            IBoneThicknessAnalysisGenerationHelper logicHelper): base(console, logicHelper)
        {
            _specificLogicHelper = logicHelper;
        }

        private LogicStatus GetThicknessData(BoneThicknessAnalysisGenerationParameters parameters, out double[] thicknessData)
        {
            if (_specificLogicHelper.GetBoneThicknessCacheData(parameters.ObjectId, out thicknessData))
            {
                return LogicStatus.Success;
            }

            console.WriteLine("Bone Thickness Analysis - Thickness data needs to be generated (only needed once, unless model is removed/updated). Generating...");

            if (!WallThicknessAnalysis.MeshWallThicknessInMM(console, parameters.BoneMesh, out thicknessData))
            {
                console.WriteErrorLine("Bone Thickness Analysis - Failed to generate the thickness data");
                return LogicStatus.Failure;
            }

            console.WriteLine("Bone Thickness Analysis - generated the thickness data successfully");

            _specificLogicHelper.SetBoneThicknessCacheData(parameters.ObjectId, thicknessData);

            return LogicStatus.Success;
        }


        protected override LogicStatus OnExecute(BoneThicknessAnalysisGenerationParameters parameters, out BoneThicknessAnalysisGenerationResult result)
        {
            result = new BoneThicknessAnalysisGenerationResult();

            var status = GetThicknessData(parameters, out var thicknessData);
            if (status != LogicStatus.Success)
            {
                return status;
            }
            
            result.ThicknessData = thicknessData;

            return LogicStatus.Success;
        }
    }
}
