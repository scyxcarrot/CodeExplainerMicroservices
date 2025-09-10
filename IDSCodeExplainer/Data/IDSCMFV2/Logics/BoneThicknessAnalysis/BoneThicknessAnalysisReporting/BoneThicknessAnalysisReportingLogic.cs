using IDS.Core.V2.Logic;
using IDS.Interface.Logic;
using IDS.Interface.Tools;

namespace IDS.CMF.V2.Logics
{
    public class BoneThicknessAnalysisReportingLogic: 
        Logic<BoneThicknessAnalysisReportingParameters, BoneThicknessAnalysisReportingResult>
    {
        private readonly IBoneThicknessAnalysisReportingHelper _specificLogicHelper;

        public BoneThicknessAnalysisReportingLogic(IConsole console,
            IBoneThicknessAnalysisReportingHelper logicHelper): base(console, logicHelper)
        {
            _specificLogicHelper = logicHelper;
        }

        protected override LogicStatus OnExecute(BoneThicknessAnalysisReportingParameters parameters, out BoneThicknessAnalysisReportingResult result)
        {
            result = new BoneThicknessAnalysisReportingResult();

            _specificLogicHelper.SetBuildingBlockThicknessMinMax(parameters.ObjectId,
                parameters.MinWallThickness, parameters.MaxWallThickness);

            MeshAnalysisUtilities.ConstraintThicknessData(parameters.ThicknessData, parameters.MinWallThickness, parameters.MaxWallThickness,
                out var constraintThicknessData, out var lowerBound, out var upperBound);

            MeshAnalysisUtilities.CreateTriangleDiagnosticMesh(parameters.BoneMesh, lowerBound, upperBound,
                constraintThicknessData, parameters.DefaultColor, out var newMesh, out var verticesColors);

            result.NewBoneMesh = newMesh;
            result.VerticesColors = verticesColors;
            result.LowerBound = lowerBound;
            result.UpperBound = upperBound;

            return LogicStatus.Success;
        }
    }
}
