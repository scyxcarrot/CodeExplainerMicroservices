using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.PICMF.Drawing;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("9f9af50d-6993-4d3b-9d00-4e45fa9b364a")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide)]
    public class CMFDeleteGuideSurfaces : CMFDrawEditGuideBase
    {
        static CMFDeleteGuideSurfaces _instance;
        public CMFDeleteGuideSurfaces()
        {
            _instance = this;
            VisualizationComponent = new DeleteGuideSurfacesVisualization();
        }

        ///<summary>The only instance of the CMFDeleteGuideSurfaces command.</summary>
        public static CMFDeleteGuideSurfaces Instance => _instance;

        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper { get; } =
            new CMFGuidePrefPanelVisualizationHelper();

        public override string EnglishName => "CMFDeleteGuideSurfaces";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var prefId = PromptForPreferenceId();
            if (prefId == Guid.Empty)
            {
                return Result.Failure;
            }

            var objManager = new CMFObjectManager(director);
            var prefDataModel = objManager.GetGuidePreference(prefId);

            GuidePrefPanelVisualizationHelper.GuidePrefPanelOpVisualization(prefDataModel, doc, false, true);

            Locking.LockAll(doc);
            UnLockPatches(doc, director, prefDataModel);
            UnLockLinks(doc, director, prefDataModel);
            UnlockSolids(doc, director, prefDataModel);

            var deleteSurfaceHelper = new DeleteGuideSurfaceHelper(doc, director);

            if (!deleteSurfaceHelper.Execute(prefDataModel))
            {
                deleteSurfaceHelper.RestoreSurfaces(ref prefDataModel);
                return Result.Cancel;
            }

            var result = deleteSurfaceHelper.Result;
            if (result == null)
            {
                deleteSurfaceHelper.RestoreSurfaces(ref prefDataModel);
                return Result.Failure;
            }

            var lowLoDConstraintMesh = GetLowLoDConstraintMesh(director);

            var roiDefiner = new Mesh();
            result.GuideBaseNegativeSurfaces.ForEach(x => roiDefiner.Append(x.Patch));
            result.GuideBaseSurfaces.ForEach(x => roiDefiner.Append(x.Patch));
            result.GuideLinkSurfaces.ForEach(x => roiDefiner.Append(x.Patch));
            result.GuideSolidSurfaces.ForEach(x => roiDefiner.Append(x.Patch));

            if (result.GuideBaseNegativeSurfaces.Any() || result.GuideBaseSurfaces.Any() || result.GuideLinkSurfaces.Any() || result.GuideSolidSurfaces.Any())
            {
                lowLoDConstraintMesh = GuideDrawingUtilities.CreateRoiMesh(lowLoDConstraintMesh, roiDefiner);
            }

            var success = ProcessDeleteGuideResult(director, lowLoDConstraintMesh, result, prefDataModel);
            if (!success)
            {
                deleteSurfaceHelper.RestoreSurfaces(ref prefDataModel);
                return Result.Failure;
            }

            ClearHistory(doc);
            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            Locking.LockAll(doc);
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
            doc.Objects.UnselectAll();
            Locking.LockAll(doc);
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
        }
    }
}
