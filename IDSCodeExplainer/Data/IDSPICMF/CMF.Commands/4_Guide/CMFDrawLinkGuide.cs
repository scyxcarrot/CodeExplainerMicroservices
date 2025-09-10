using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Drawing;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("E01FE20B-F3E4-4BB1-8ED7-D1C667F60510")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideSurfaceWrap)]
    public class CMFDrawLinkGuide : CMFDrawEditGuideBase
    {
        static CMFDrawLinkGuide _instance;
        public CMFDrawLinkGuide()
        {
            _instance = this;
            VisualizationComponent = new GuideAndLinkVisualization();
        }
        
        public static CMFDrawLinkGuide Instance => _instance;

        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper { get; } =
            new CMFGuidePrefPanelVisualizationHelper();

        public override string EnglishName => "CMFDrawLinkGuide";
        
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
            var positiveGuideDrawingEibb = guideComponent.GetGuideBuildingBlock(IBB.PositiveGuideDrawings, guidePrefModel);

            if (objectManager.HasBuildingBlock(positiveGuideDrawingEibb))
            {
                return true;
            }

            IDSPluginHelper.WriteLine(LogCategory.Warning, "CMFDrawLinkGuide requires at least one PositiveGuideDrawings for the same Guide to execute.");
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

            GuidePrefPanelVisualizationHelper.GuidePrefPanelOpVisualization(guidePrefModel, doc, false, true);

            Mesh lowLoDConstraintMesh;
            DrawGuideResult drawGuideResult;
            var drawGuideSuccess = HandleGuideDrawing(director, prefId, out lowLoDConstraintMesh, out drawGuideResult);
            if (drawGuideSuccess != Result.Success)
            {
                return drawGuideSuccess;
            }

            var success = ProcessDrawResult(director, drawGuideResult, guidePrefModel);
            if (!success)
            {
                return Result.Failure;
            }

            ClearHistory(doc);
            return Result.Success;
        }
        
        private Result HandleGuideDrawing(CMFImplantDirector director, Guid prefId, out Mesh lowLoDConstraintMesh, out DrawGuideResult drawingResult)
        {
            RhinoApp.SetFocusToMainWindow(director.Document);

            lowLoDConstraintMesh = null;
            drawingResult = null;

            var factory = new DrawGuideDataContextFactory();
            var dataContext = factory.CreateDrawGuideDataContextForGuideLink();            
            if (!InitializeDataContext(ref dataContext, false, prefId, director))
            {
                return Result.Cancel;
            }

            lowLoDConstraintMesh = GetLowLoDConstraintMesh(director);

            var drawGuide = new DrawGuide(lowLoDConstraintMesh, GetGuideSurfaceCreationLowLoDBaseModel(director), dataContext, false);
            var prompt = "Press P to switch mode to Patch or Skeleton drawing. O to toggle support mesh transparency On/Off. " +
                         "In Skeleton mode, press L to start a new Skeleton drawing.";
            drawGuide.SetCommandPrompt(prompt);

            if (!drawGuide.Execute())
            {
                return Result.Failure;
            }
            
            drawingResult = drawGuide.ResultOfGuideDrawing;
            if (drawingResult == null)
            {
                return Result.Failure;
            }

            return Result.Success;
        }

        private bool ProcessDrawResult(CMFImplantDirector director, DrawGuideResult drawGuideResult, GuidePreferenceDataModel guidePrefModel)
        {
            var linkSurfacesResult = drawGuideResult.GuideBaseSurfaces.ToList();           

            return ProcessDrawResult(director, drawGuideResult.RoIMesh, new List<PatchData>(), new List<PatchData>(), linkSurfacesResult, new List<PatchData>(), guidePrefModel);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
        }
    }
}
