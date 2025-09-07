using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using IDS.Glenius.CommandHelpers;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Linq;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("173e9bb0-ca80-49ac-8962-579501d275d5")]
    [IDSGleniusCommand(DesignPhase.Reconstruction, IBB.Scapula, IBB.DefectRegionCurves)]
    public class GleniusEditDefectRegion : CommandBase<GleniusImplantDirector>
    {
        public GleniusEditDefectRegion()
        {
            Instance = this;
            VisualizationComponent = new DrawEditDefectRegionVisualization();
        }

        ///<summary>The only instance of the GleniusEditDefectRegion command.</summary>
        public static GleniusEditDefectRegion Instance { get; private set; }

        public override string EnglishName => "GleniusEditDefectRegion";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {
            //Get scapula mesh
            var objManager = new GleniusObjectManager(director);
            var scapula = objManager.GetBuildingBlock(IBB.Scapula).Geometry as Mesh;

            var defectRegionCurves = objManager.GetAllBuildingBlocks(IBB.DefectRegionCurves).ToArray();

            Locking.UnlockDefectRegionCurves(doc);

            //same command state, it shows old curve. If all fails, fall back do edit one by one per command.
            var escapeFromDeletion = false;
            while (true)
            {
                Locking.UnlockDefectRegionCurves(doc);
                doc.Views.Redraw();

                var getCurve = new GetObject();
                getCurve.SetCommandPrompt("Select defect region curve to edit");
                getCurve.GeometryFilter = ObjectType.Curve;
                getCurve.DisablePreSelect();
                getCurve.AcceptNothing(true);
                getCurve.SubObjectSelect = false;
                getCurve.EnableTransparentCommands(false);

                var res = getCurve.Get();

                if (res == GetResult.Object)
                {
                    var obj = Array.Find(defectRegionCurves, c => c.Id == getCurve.Object(0).ObjectId);

                    var guid = obj.Id;

                    var drawCurve = new EditCurveDeletable(doc, guid, true, false);
                    drawCurve.ConstraintMesh = scapula;
                    drawCurve.AlwaysOnTop = true;
                    drawCurve.AcceptNothing(true); // Pressing ENTER is allowed
                    drawCurve.AcceptUndo(true); // Enables ctrl-z

                    RhinoApp.WriteLine("Edit Curve additional operations:Shift + Mouse Left-click - to add Additional Point within a curve; Alt + Mouse Left-click - to remove the selected point within a curve; delete - to remove the selected curve.");
                    var edited = drawCurve.Edit();
                    if (edited)
                    {
                        defectRegionCurves = objManager.GetAllBuildingBlocks(IBB.DefectRegionCurves).ToArray();

                        //Remove Healthy and Reconstructed if present  [Dependency]
                        var helper = new ReconstructionHelper(director);
                        helper.DeleteExecuteReconstructionRelatedObjects();

                        doc.Views.Redraw();
                    }

                    escapeFromDeletion = edited && drawCurve.DeleteCurveRequested;
                }
                else if (escapeFromDeletion)
                {
                    escapeFromDeletion = false;
                }
                else if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    break;
                }
            }
            //Lock all curves
            Core.Operations.Locking.LockAll(doc);

            return Result.Success;
        }
    }
}
