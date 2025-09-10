using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.PICMF.Drawing;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using System;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("e89fbe24-62a8-465e-ace7-cd9e97ac0bf1")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.GuideSurfaceWrap)]
    public class CMFEditGuide : CMFDrawEditGuideBase
    {
        static CMFEditGuide _instance;

        public CMFEditGuide()
        {
            _instance = this;
            VisualizationComponent = new DrawGuideVisualization();
        }

        ///<summary>The only instance of the CMFEditGuide command.</summary>
        public static CMFEditGuide Instance => _instance;

        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper { get; } =
            new CMFGuidePrefPanelVisualizationHelper();

        public override string EnglishName => "CMFEditGuide";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var prefId = PromptForPreferenceId();
            if (prefId == Guid.Empty)
            {
                return Result.Failure;
            }

            var objManager = new CMFObjectManager(director);
            var prefDataModel = objManager.GetGuidePreference(prefId);
            var allDataModel = objManager.GetAllGuidePreferenceData();

            foreach (var guideDataModel in allDataModel)
            {
                var isComponentVisible = guideDataModel.CaseGuid == prefDataModel.CaseGuid;
                VisualizeAllGuideComponents(doc, director, guideDataModel, false, false, !isComponentVisible, isComponentVisible);
            }

            var lowLoDConstraintMesh = GetLowLoDConstraintMesh(director);
            var lowLoDBaseForGuideSurfaceCreation = GetGuideSurfaceCreationLowLoDBaseModel(director);

            var editGuide = new EditGuideHelper(lowLoDConstraintMesh, lowLoDBaseForGuideSurfaceCreation, doc, director);
            
            UnLockPatches(doc, director, prefDataModel);

            if (!editGuide.Execute(prefDataModel))
            {
                return Result.Failure;
            }

            var result = editGuide.ResultOfGuideDrawing;
            if (result == null)
            {
                return Result.Failure;
            }

            var success = ProcessAppendedGuideResult(director, result.RoIMesh, result, prefDataModel);
            if (!success)
            {
                return Result.Failure;
            }

            ClearHistory(doc);
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            var objManager = new CMFObjectManager(director);
            var allDataModel = objManager.GetAllGuidePreferenceData();

            foreach (var guideDataModel in allDataModel)
            {
                VisualizeAllGuideComponents(doc, director, guideDataModel, true, false, true, true);
            }

            doc.Objects.UnselectAll();
            Locking.LockAll(doc);
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            var objManager = new CMFObjectManager(director);
            var allDataModel = objManager.GetAllGuidePreferenceData();

            foreach (var guideDataModel in allDataModel)
            {
                VisualizeAllGuideComponents(doc, director, guideDataModel, true, false, true, true);
            }

            doc.Objects.UnselectAll();
            Locking.LockAll(doc);
            doc.Views.Redraw();
        }

        private void VisualizeAllGuideComponents(RhinoDoc doc, CMFImplantDirector director, GuidePreferenceDataModel guideDataModel, 
            bool isVisible, bool restoreVisualization, bool applyVisibilityToAllGuideComponents, bool isShowBarrel)
        {
            GuidePrefPanelVisualizationHelper.GuidePrefPanelOpVisualization(guideDataModel,
                                                                            doc,
                                                                            isVisible,
                                                                            restoreVisualization,
                                                                            applyVisibilityToAllGuideComponents);
            GuidePrefPanelVisualizationHelper.ShowBarrels(director, guideDataModel, isShowBarrel);
        }
    }
}
