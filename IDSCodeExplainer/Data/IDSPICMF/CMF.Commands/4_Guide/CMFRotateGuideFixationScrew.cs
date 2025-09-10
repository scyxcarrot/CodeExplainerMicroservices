using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Constants;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("53dd640d-3d43-4005-a9c2-520fde3bd2e9")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Guide, IBB.GuideFixationScrew)]
    public class CMFRotateGuideFixationScrew : CmfCommandBase
    {
        static CMFRotateGuideFixationScrew _instance;
        public CMFRotateGuideFixationScrew()
        {
            _instance = this;
            VisualizationComponent = new CMFManipulateGuideFixationScrewVisualization();
        }

        ///<summary>The only instance of the CMFRotateGuideFixationScrew command.</summary>
        public static CMFRotateGuideFixationScrew Instance => _instance;

        public override string EnglishName => "CMFRotateGuideFixationScrew";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            // Unlock screws
            Locking.UnlockGuideFixationScrewsExceptShared(director);

            var selectScrew = new GetObject();
            selectScrew.SetCommandPrompt("Select a screw to rotate.");
            selectScrew.EnablePreSelect(false, false);
            selectScrew.EnablePostSelect(true);
            selectScrew.AcceptNothing(true);
            selectScrew.EnableTransparentCommands(false);

            var res = selectScrew.Get();
            if (res == GetResult.Object)
            {
                var objectManager = new CMFObjectManager(director);

                var constraintRhObj = objectManager.GetBuildingBlock(IBB.GuideSurfaceWrap);

                var constraintMesh = (Mesh)constraintRhObj.Geometry;

                // Get selected screw
                var screw = (Screw)selectScrew.Object(0).Object();
                var centerOfRotation =
                    PointUtilities.GetRayIntersection(constraintMesh, screw.HeadPoint, screw.Direction);
                var averageNormalAtCenterOfRotation = VectorUtilities.FindAverageNormal(constraintMesh, 
                    centerOfRotation, ScrewAngulationConstants.AverageNormalRadiusGuideFixationScrew);

                var operation = new RotateGuideFixationScrew(screw, centerOfRotation, -averageNormalAtCenterOfRotation)
                {
                    ConstraintMesh = constraintMesh
                };

                var result = operation.Rotate(false);
                director.Document.Objects.UnselectAll();
                director.Document.Views.Redraw();

                var screwNumber = ScrewUtilities.GetScrewNumberWithPhaseNumber(screw, true);
                TrackingParameters.Add("Affected Screw", screwNumber);

                if (result == Result.Success && operation.NeedToClearUndoRedoRecords)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, "Rotated a shared screw will clear Undo/Redo upon success, and you will no longer able undo to your previous operations.");
                    doc.ClearUndoRecords(true);
                    doc.ClearRedoRecords();
                    director.IdsDocument?.ClearUndoRedo();
                }

                return result;
            }

            return Result.Failure;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, CMFImplantDirector director)
        {
            doc.Objects.UnselectAll();
            doc.Views.Redraw();
        }
    }
}
