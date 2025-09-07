using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.V2.Constants;
using IDS.CMF.V2.Logics;
using IDS.CMF.Visualization;
using IDS.Core.Plugin;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("c1d023d4-9ebf-4029-a507-4a42082433d1")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any)]
    public class CMFBoneThicknessAnalysis : CmfCommandBase
    {
        static CMFBoneThicknessAnalysis _instance;
        public CMFBoneThicknessAnalysis()
        {
            _instance = this;
        }

        ///<summary>The only instance of the CMFBoneThicknessAnalysis command.</summary>
        public static CMFBoneThicknessAnalysis Instance => _instance;

        public override string EnglishName => "CMFBoneThicknessAnalysis";

        private const double DefaultMinWallThickness = BoneThicknessAnalysisConstants.MinMinWallThickness;
        private double _currentMinWallThickness = DefaultMinWallThickness;
        private double _currentMaxWallThickness = BoneThicknessAnalysisConstants.DefaultMaxWallThickness;

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (BoneThicknessAnalyzableObjectManager.CheckIfGotVertexColor(doc))
            {
                BoneThicknessAnalyzableObjectManager.HandleRemoveAllVertexColor(director);
                doc.Views.Redraw();
                return Result.Success;
            }

            var logicHelper = new BoneThicknessAnalysisIntegrateHelper(director, _currentMinWallThickness, _currentMaxWallThickness);
            var logic = new BoneThicknessAnalysisIntegrateLogic(new IDSRhinoConsole(), logicHelper);
            var status = logic.Execute(out _);

            _currentMinWallThickness = logicHelper.CurrentMinWallThickness;
            _currentMaxWallThickness = logicHelper.CurrentMaxWallThickness;

            return status.ToResultStatus();
        }
    }
}
