using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Drawing;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("A064EFF1-B0AE-4CB5-9BF3-DBF5A1780C5C")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideSurfaceWrap)]
    public class CMFEditLinkGuide : CMFDrawEditGuideBase
    {
        static CMFEditLinkGuide _instance;
        public CMFEditLinkGuide()
        {
            _instance = this;
            VisualizationComponent = new GuideAndLinkVisualization();
        }
        
        public static CMFEditLinkGuide Instance => _instance;

        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper =
            new CMFGuidePrefPanelVisualizationHelper();

        public override string EnglishName => "CMFEditLinkGuide";
        
        private Guid SelectedGuidePreferenceID = Guid.Empty;

        public override bool CheckCommandCanExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            if (!base.CheckCommandCanExecute(doc, mode, director))
            {
                return false;
            }

            SelectedGuidePreferenceID = PromptForPreferenceId();
            if (SelectedGuidePreferenceID == Guid.Empty)
            {
                return false;
            }            

            var objectManager = new CMFObjectManager(director);
            var guidePrefModel = objectManager.GetGuidePreference(SelectedGuidePreferenceID);

            var guideComponent = new GuideCaseComponent();
            var guideLinkSurfaceEibb = guideComponent.GetGuideBuildingBlock(IBB.GuideLinkSurface, guidePrefModel);

            if (objectManager.HasBuildingBlock(guideLinkSurfaceEibb))
            {
                return true;
            }

            IDSPluginHelper.WriteLine(LogCategory.Warning, "CMFEditLinkGuide requires at least one GuideLinkSurface for the same Guide to execute.");
            return false;
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var prefId = SelectedGuidePreferenceID;
            if (prefId == Guid.Empty)
            {
                return Result.Failure;
            }

            var objManager = new CMFObjectManager(director);
            var guidePrefModel = objManager.GetGuidePreference(prefId);
            var allDataModel = objManager.GetAllGuidePreferenceData();

            foreach (var guideDataModel in allDataModel)
            {
                var isComponentVisible = guideDataModel.CaseGuid == guidePrefModel.CaseGuid;
                VisualizeAllGuideComponents(doc, director, guideDataModel, false, false, !isComponentVisible, isComponentVisible);
            }


            Mesh lowLoDConstraintMesh;
            DrawGuideResult editGuideResult;
            var editGuideSuccess = HandleEditDrawing(director, guidePrefModel, out lowLoDConstraintMesh, out editGuideResult);
            if (editGuideSuccess != Result.Success)
            {
                return editGuideSuccess;
            }

            var success = ProcessAppendedGuideLinkResult(director, editGuideResult.RoIMesh, editGuideResult, guidePrefModel);
            if (!success)
            {
                return Result.Failure;
            }

            ClearHistory(doc);
            return Result.Success;
        }
        
        private Result HandleEditDrawing(CMFImplantDirector director, GuidePreferenceDataModel guidePrefModel, out Mesh lowLoDConstraintMesh, out DrawGuideResult editingResult)
        {
            RhinoApp.SetFocusToMainWindow(director.Document);

            lowLoDConstraintMesh = GetLowLoDConstraintMesh(director);
            var lowLoDBaseForGuideSurfaceCreation = GetGuideSurfaceCreationLowLoDBaseModel(director);
            editingResult = null;

            Locking.LockAll(director.Document);
            UnLockLinks(director.Document, director, guidePrefModel);

            var editGuide = new EditGuideHelper(lowLoDConstraintMesh, lowLoDBaseForGuideSurfaceCreation, director.Document, director);
            if (!editGuide.ExecuteEditLink(guidePrefModel))
            {
                return Result.Failure;
            }
            
            editingResult = editGuide.ResultOfGuideDrawing;
            if (editingResult == null)
            {
                return Result.Failure;
            }

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
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            var objManager = new CMFObjectManager(director);
            var allDataModel = objManager.GetAllGuidePreferenceData();

            foreach (var guideDataModel in allDataModel)
            {
                VisualizeAllGuideComponents(doc, director, guideDataModel, true, false, true, true);
            }
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
