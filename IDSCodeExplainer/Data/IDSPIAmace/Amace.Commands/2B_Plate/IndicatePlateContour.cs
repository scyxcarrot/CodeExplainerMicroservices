using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Common;
using IDS.Common.Visualization;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace IDS.Amace.Commands
{
    /**
     * Rhino command to indicate the bottom plate contour.
      * The contour is indicated on the "bottom ballpark surface".
    */

    [System.Runtime.InteropServices.Guid("be1a513c-5013-416a-88d5-e6fee3e597e7")]
    [IDSCommandAttributes(true, DesignPhase.Plate, IBB.SkirtCupCurve, IBB.WrapBottom, IBB.Cup)]
    public class IndicatePlateContour : CommandBase<ImplantDirector>
    {
        public IndicatePlateContour()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        /// The one and only instance of this command
        public static IndicatePlateContour TheCommand { get; private set; }

        /// The command name as it appears on the Rhino command line
        public override string EnglishName => "IndicatePlateContour";

        private readonly Dependencies _dependencies;

        /**
        * Run the IndBottomPlateCont command
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {

            // Ask user what he wants to do with curve
            var gm = new GetOption();
            gm.SetCommandPrompt("Select curve indication/edit mode");
            gm.AcceptNothing(false);
            var optSide = new OptionToggle(false, "Bottom", "Top");
            var optIdSide = gm.AddOptionToggle("Side", ref optSide);
            var modeIndicate = gm.AddOption("Indicate");
            var modePoints = gm.AddOption("EditPoints");
            var modeGenerateOther = gm.AddOption("GenerateOther");
            while (true)
            {
                var gres = gm.Get();
                if (gres == GetResult.Cancel)
                {
                    return Result.Failure;
                }

                if (gres == GetResult.Option)
                {
                    if (gm.OptionIndex() == optIdSide)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }

                }
            }
            var modeSelected = gm.OptionIndex();
            var top = optSide.CurrentValue;
            var contourBlockType = top ? IBB.PlateContourTop : IBB.PlateContourBottom;
            var wrapTarget = top ? IBB.WrapTop : IBB.WrapBottom;

            // Get supporting data
            var cup = director.cup;
            var screwManager = new ScrewManager(director.Document);
            var screws = screwManager.GetAllScrews();
            var objectManager = new AmaceObjectManager(director);
            var oldContourId = objectManager.GetBuildingBlockId(contourBlockType);
            var targetMesh = objectManager.GetBuildingBlock(wrapTarget).Geometry as Mesh;

            // Can't change contour if it does not exist
            if (oldContourId == Guid.Empty)
            {
                modeSelected = modeIndicate;
                IDSPIAmacePlugIn.WriteLine(LogCategory.Warning, "No curve currently exists. Indicate a new one.");
            }

            // Turn conduit off
            var conduitWasOn = Proxies.TogglePlateAnglesVisualisation.Enabled;
            if (conduitWasOn)
            {
                Proxies.TogglePlateAnglesVisualisation.Disable(director);
            }

            // Set appropriate view
            if (top)
            {
                Visualization.Visibility.EditTopPlateCurve(doc);
            }
            else
            {
                Visualization.Visibility.EditBottomPlateCurve(doc);
            }

            // Indicate/edit curve
            Curve contour;
            if (modeSelected == modeIndicate)
            {
                // Indicate contour
                var success = DrawPlateContour(doc, targetMesh, screws, top, out contour);
                if (!success)
                {
                    return Result.Failure;
                }

                // If you indicated the top contour, project to bottom wrap to initialize bottom contour
                if (top)
                {
                    var wrapBottomObj = (MeshObject)objectManager.GetBuildingBlock(IBB.WrapBottom);
                    var wrapBottom = wrapBottomObj.MeshGeometry;
                    var contourBottom = CurveUtilities.ProjectContourToMesh(wrapBottom, contour);
                    var oldContourBottomId = objectManager.GetBuildingBlockId(IBB.PlateContourBottom);
                    objectManager.SetBuildingBlock(IBB.PlateContourBottom, contourBottom, oldContourBottomId);
                }
            }
            else if (modeSelected == modePoints)
            {
                var oldCrv = doc.Objects.Find(oldContourId).Geometry as Curve;
                if (null == oldCrv)
                {
                    return Result.Failure;
                }

                var success = DrawPlateContour(doc, targetMesh, screws, top, out contour, oldCrv);
                if (!success)
                {
                    return Result.Failure;
                }
            }
            else if (modeSelected == modeGenerateOther)
            {
                if (top)
                {
                    var topCurve = (Curve)doc.Objects.Find(objectManager.GetBuildingBlockId(IBB.PlateContourTop))
                        .Geometry;
                    var wrapBottomObj = (MeshObject)objectManager.GetBuildingBlock(IBB.WrapBottom);
                    var wrapBottom = wrapBottomObj.MeshGeometry;
                    var contourBottom = CurveUtilities.ProjectContourToMesh(wrapBottom, topCurve);
                    var oldContourBottomId = objectManager.GetBuildingBlockId(IBB.PlateContourBottom);
                    objectManager.SetBuildingBlock(IBB.PlateContourBottom, contourBottom, oldContourBottomId);
                    RhinoApp.WriteLine("Plate bottom contour has been regenerated from top contour.");
                    return CommandSucceeded(director, doc, IBB.PlateContourBottom, conduitWasOn);
                }

                var bottomCurve =
                    (Curve)doc.Objects.Find(objectManager.GetBuildingBlockId(IBB.PlateContourBottom)).Geometry;
                var wrapTopObj = (MeshObject)objectManager.GetBuildingBlock(IBB.WrapTop);
                var wrapTop = wrapTopObj.MeshGeometry;
                var contourTop = CurveUtilities.ProjectContourToMesh(wrapTop, bottomCurve);
                var oldContourTopId = objectManager.GetBuildingBlockId(IBB.PlateContourTop);
                objectManager.SetBuildingBlock(IBB.PlateContourTop, contourTop, oldContourTopId);
                RhinoApp.WriteLine("Plate top contour has been regenerated from bottom contour.");
                return CommandSucceeded(director, doc, IBB.PlateContourTop, conduitWasOn);
            }
            else
            {
                return Result.Failure;
            }

            // Set created contour to building block
            objectManager.SetBuildingBlock(contourBlockType, contour, oldContourId);

            // Succes
            RhinoApp.WriteLine("Plate bottom contour has been added. Contour can now be edited if desired.");

            return CommandSucceeded(director, doc, contourBlockType, conduitWasOn);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            Visualization.Visibility.PlateDefault(doc);
        }

        private Result CommandSucceeded(ImplantDirector director, RhinoDoc doc, IBB contourBlockType, bool conduitWasOn)
        {
            // Delete dependencies
            _dependencies.DeleteBlockDependencies(director, contourBlockType);
            _dependencies.DeleteTrimmedBumps(director);

            if (conduitWasOn)
            {
                Proxies.TogglePlateAnglesVisualisation.Enable(director);
            }
            else
            {
                Visualization.Visibility.PlateDefault(doc);
            }

            return Result.Success;
        }

        /**
         * Indicate the plate contour on a larger mesh from which you want to cut out a patch.
         * @param constrainingGeometry  This may contain a one of each geometry type that
         *                              point picking can be constrained to. I.e. one mesh,
         *                              one Brep, and one Curve.
         * @param embeddedScrews        Screws that will be embedded in the plate.
         * @param cup                   Cup that will be embedded in the plate.
         * @param[out] contour          Closed contour curve indicated by user.
         * @return                      true on success, false on failure
         */

        public static bool DrawPlateContour(RhinoDoc doc, Mesh targetMesh, IEnumerable<Screw> screws, bool drawTop, out Curve contour, Curve initialCurve = null)
        {
            // Get the constraining geometry
            contour = null;

            // Get director
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            if (director == null)
            {
                return false;
            }

            // Get snap points and safety volumes for all the screws in the document
            var entOutlineAx = new List<Line>();
            var entOutlineRad = new List<double>();
            var snapCurveIDs = new List<Guid>();
            foreach (var screw in screws)
            {
                if (screw.positioning == ScrewPosition.Flange)
                {
                    snapCurveIDs.Add(doc.Objects.AddCurve(screw.PlateOutline(targetMesh)));
                    entOutlineAx.Add(screw.ScrewLine);
                    entOutlineRad.Add(screw.OutlineRadius);
                }
            }

            // Show the Outline Entities
            doc.Views.Redraw();

            // Indicate contour curve on target mesh
            var objectManager = new ObjectManager(director);
            var gp = new DrawPlate(director.Document, director, objectManager);
            if (initialCurve != null)
            {
                gp.SetExistingCurve(initialCurve, true, false);
            }

            gp.SetCommandPrompt("Indicate contour points. Press ENTER to finish or ctrl+z to undo.");
            gp.AcceptNothing(true); // Pressing ENTER is allowed
            gp.AcceptUndo(true); // Enables ctrl-z
            gp.PermitObjectSnap(true); // Only allow our own constraining geometry
            gp.ConstraintMesh = targetMesh;

            contour = drawTop ? gp.DrawTop() : gp.DrawBottom();
            foreach (var id in snapCurveIDs)
            {
                doc.Objects.Delete(id, true);
            }

            return (null != contour);
        }
    }
}