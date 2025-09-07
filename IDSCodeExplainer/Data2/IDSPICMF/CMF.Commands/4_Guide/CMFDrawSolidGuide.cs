using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using IDS.CMF;
using IDS.CMF.Utilities;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.CommandHelpers;
using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.PICMF.Drawing;
using IDS.PICMF.Visualization;
using System;
using System.Linq;
using System.Collections.Generic;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("E3A1C5B9-3739-4BA3-99A2-D18326BF95D2")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideSurfaceWrap)]
    public class CMFDrawSolidGuide : CMFDrawEditGuideBase
    {
        public override string EnglishName => "CMFDrawSolidGuide";
        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper { get; } =
            new CMFGuidePrefPanelVisualizationHelper();

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
            var dataContext = factory.CreateDrawGuideDataContextForGuideSolid();
            if (!InitializeDataContext(ref dataContext, true, prefId, director, true))
            {
                return Result.Cancel;
            }

            lowLoDConstraintMesh = GetLowLoDConstraintMesh(director);

            var drawGuide = new DrawGuide(lowLoDConstraintMesh, GetGuideSurfaceCreationLowLoDBaseModel(director), dataContext, false, true, true);
            var prompt = "Currently drawing Solid Surfaces. O to toggle support mesh transparency On/Off. ";
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
            var solidSurfacesResult = drawGuideResult.GuideBaseSurfaces.ToList();

            return ProcessDrawResult(director, drawGuideResult.RoIMesh, new List<PatchData>(), new List<PatchData>(), new List<PatchData>(), solidSurfacesResult, guidePrefModel);
        }
    }
}