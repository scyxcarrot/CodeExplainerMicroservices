using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
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
    [System.Runtime.InteropServices.Guid("d1bebdd3-7248-4644-834e-037f799870d4")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.GuideSurfaceWrap)]
    public class CMFDrawGuide : CMFDrawEditGuideBase
    {
        static CMFDrawGuide _instance;
        public CMFDrawGuide()
        {
            _instance = this;
            VisualizationComponent = new DrawGuideVisualization();
        }

        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper { get; } =
            new CMFGuidePrefPanelVisualizationHelper();

        ///<summary>The only instance of the CMFDrawGuide command.</summary>
        public static CMFDrawGuide Instance => _instance;

        public override string EnglishName => "CMFDrawGuide";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var prefId = PromptForPreferenceId();
            if (prefId == Guid.Empty)
            {
                return Result.Failure;
            }

            var objManager = new CMFObjectManager(director);
            var guidePrefModel = objManager.GetGuidePreference(prefId);

            GuidePrefPanelVisualizationHelper.GuidePrefPanelOpVisualization(guidePrefModel, doc, false, true);

            //Check if there is/or no osteotomy planes on original position to inform the user.
            var originalOsteotomies = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(doc);
            if (!originalOsteotomies.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Missing Osteotomy Plane in Original Position, the surfaces created will not have osteotomy cut.");
            }

            Mesh lowLoDConstraintMesh;
            DrawGuideResult drawGuideResult;
            var drawGuideSuccess = HandleGuideDrawing(director, prefId, out lowLoDConstraintMesh, out drawGuideResult);
            if (drawGuideSuccess != Result.Success)
            {
                return drawGuideSuccess;
            }

            var success = ProcessDrawGuideResult(director, drawGuideResult.RoIMesh, drawGuideResult, guidePrefModel);
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
            var dataContext = factory.CreateDrawGuideDataContextForGuideSurface();
            if (!InitializeDataContext(ref dataContext, true, prefId, director))
            {
                return Result.Cancel;
            }

            lowLoDConstraintMesh = GetLowLoDConstraintMesh(director);

            var drawGuide = new DrawGuide(lowLoDConstraintMesh, GetGuideSurfaceCreationLowLoDBaseModel(director), dataContext, true);
            var prompt = "Press P to switch mode to Patch or Skeleton drawing. O to toggle support mesh transparency On/Off. " +
                         "In Patch mode, press L to switch between Positive and Negative Patch drawing. " +
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

        private bool ProcessDrawGuideResult(CMFImplantDirector director, Mesh mesh, DrawGuideResult drawGuideResult, GuidePreferenceDataModel guidePrefModel)
        {
            var positiveSurfaces = drawGuideResult.GuideBaseSurfaces.ToList();
            var negativeSurfaces = drawGuideResult.GuideBaseNegativeSurfaces.ToList();

            return ProcessDrawResult(director, mesh, positiveSurfaces, negativeSurfaces, new List<PatchData>(), new List<PatchData>(), guidePrefModel);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
        }
    }
}
