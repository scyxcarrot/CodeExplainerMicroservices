using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Helper;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("a3634879-a325-4243-ab09-bdce4d9beb52")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideSupport)]
    public class CMFPlaceGuideFixationScrew : CmfCommandBase
    {
        static CMFPlaceGuideFixationScrew _instance;
        public CMFPlaceGuideFixationScrew()
        {
            _instance = this;
            VisualizationComponent = new CMFPlaceGuideFixationScrewVisualization();
        }

        ///<summary>The only instance of the CMFPlaceGuideFixationScrew command.</summary>
        public static CMFPlaceGuideFixationScrew Instance => _instance;

        public CMFGuidePrefPanelVisualizationHelper GuidePrefPanelVisualizationHelper { get; } =
            new CMFGuidePrefPanelVisualizationHelper();

        public override string EnglishName => "CMFPlaceGuideFixationScrew";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var prefId = GuidePreferencesHelper.PromptForPreferenceId();
            if (prefId == Guid.Empty)
            {
                return Result.Failure;
            }

            var objManager = new CMFObjectManager(director);
            var guidePreferenceModel = (GuidePreferenceModel)objManager.GetGuidePreference(prefId);

            GuidePrefPanelVisualizationHelper.GuidePrefPanelOpVisualization(guidePreferenceModel, doc, 
                false, true);

            RhinoLayerUtilities.DeleteEmptyLayers(doc);

            var rhinoObj = objManager.GetBuildingBlock(IBB.GuideSurfaceWrap);
            var fullLodConstraintMesh = (Mesh)rhinoObj.Geometry;

            Mesh lowLoDConstraintMesh;
            objManager.GetBuildingBlockLoDLow(rhinoObj.Id, out lowLoDConstraintMesh);

            
            var screwPlacer = new ScrewPlacer(director);
            bool closeByHasScrewButNotShared;
            var placedScrew = screwPlacer.DoPlaceGuideFixationScrew(lowLoDConstraintMesh, fullLodConstraintMesh, lowLoDConstraintMesh, guidePreferenceModel, out closeByHasScrewButNotShared);

            if (placedScrew == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Screw Leveling Failed!");
                return Result.Failure;
            }

            if (closeByHasScrewButNotShared)
            {
                Dialogs.ShowMessage("Guide fixation screw placed is not shared because it has different screw type", "Place Guide Fixation Screw", 
                    ShowMessageButton.OK, ShowMessageIcon.Exclamation);
            }

            guidePreferenceModel.Graph.InvalidateGraph();
            guidePreferenceModel.Graph.NotifyBuildingBlockHasChanged(new[] {IBB.GuideFixationScrew}, IBB.GuideFixationScrewLabelTag);

            SetScrewColor(guidePreferenceModel, placedScrew);

            doc.Objects.UnselectAll();
            doc.Views.Redraw();

            if (!placedScrew.GetScrewItSharedWith().Any())
            {
                BaseCustomUndoRedoTag = placedScrew;
                return Result.Success;
            }

            IDSPluginHelper.WriteLine(LogCategory.Default, "Placed a shared screw will clear Undo/Redo upon success, and you will no longer able undo to your previous operations.");
            doc.ClearUndoRecords(true);
            doc.ClearRedoRecords();

            return Result.Success;
        }

        private void SetScrewColor(GuidePreferenceDataModel guidePreferenceModel, Screw screw)
        {
            var color = CasePreferencesHelper.GetColor(guidePreferenceModel.NCase);

            var screwMaterial = screw.GetMaterial(true);
            screwMaterial.AmbientColor = color;
            screwMaterial.DiffuseColor = color;
            screwMaterial.SpecularColor = color;
            screwMaterial.CommitChanges();
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            RhinoLayerUtilities.DeleteEmptyLayers(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
            RhinoLayerUtilities.DeleteEmptyLayers(doc);
        }

        public override void OnCommandExecuteCanceled(RhinoDoc doc, CMFImplantDirector director)
        {
            GuidePrefPanelVisualizationHelper.RestoreVisualisation(doc, false);
            RhinoLayerUtilities.DeleteEmptyLayers(doc);
        }

        public override void OnCommandBaseCustomRedo(RhinoDoc doc, object tag)
        {
            var screw = (Screw)tag;
            screw.UpdateAidesInDocument();
        }
    }
}
