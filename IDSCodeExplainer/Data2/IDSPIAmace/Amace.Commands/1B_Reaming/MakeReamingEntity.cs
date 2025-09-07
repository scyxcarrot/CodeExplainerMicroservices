using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Drawing;
using IDS.Core.Importer;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;

namespace IDS.Commands.Reaming
{
    /**
     * Command to ream the defect pelvis using a planar extrusion block
     */

    [System.Runtime.InteropServices.Guid("c747c715-de73-4a41-b981-44f72cf05705")]
    [IDSCommandAttributes(false, DesignPhase.Reaming, IBB.DesignPelvis)]
    public class MakeReamingEntity : CommandBase<ImplantDirector>
    {
        public MakeReamingEntity()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MakeReamingEntity TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "MakeReamingEntity";

        /**
         * Let user draw a plane to ream the pelvis.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // init
            var chosenEntityId = Guid.Empty;
            bool success;

            // Let user either select plane or import plane
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Import MIMICS plane or draw/edit in document?");
            var modeImport = getOption.AddOption("MimicsImport");
            var modeDraw = getOption.AddOption("Draw");
            var modeEdit = getOption.AddOption("Edit");
            getOption.Get();
            if (getOption.CommandResult() != Result.Success)
            {
                return getOption.CommandResult();
            }
            var option = getOption.Option();
            if (null == option)
            {
                return Result.Failure;
            }

            var modeSelected = option.Index; // Index of the chosen method

            var objManager = new AmaceObjectManager(director);

            // Get the plane
            Plane reamingPlane;
            if (modeSelected == modeImport)
            {
                // Import plane from mimics
                success = PlaneImporter.ImportMimicsPlane(DirectoryStructure.GetWorkingDir(director.Document), out reamingPlane);
                if (!success)
                {
                    return Result.Failure;
                }
            }
            else if (modeSelected == modeDraw)
            {
                // Draw plane using 3-point method
                Amace.Visualization.Visibility.ReamingDefaultWithoutCupRbv(doc);
                var constrainMesh = doc.Objects.Find(objManager.GetBuildingBlockId(IBB.DesignPelvis)).Geometry as Mesh;
                var pd = new PlaneDrawer();
                success = pd.ThreePointPlane(constrainMesh, out reamingPlane);
                if (!success)
                {
                    return Result.Failure;
                }
            }
            else if (modeSelected == modeEdit)
            {
                // Choose a reaming entity and extract the plane
                Amace.Visualization.Visibility.ReamingDefaultWithoutCupRbv(doc);
                Amace.Operations.Locking.UnlockReamingEntities(director.Document);
                success = PlaneFromReamingEntity.GetPlane(doc, out chosenEntityId, out reamingPlane);
                if (!success)
                {
                    return Result.Failure;
                }
            }
            else
            {
                throw new IDSException("Mode is not Valid, This should not happen!");
            }

            Amace.Visualization.Visibility.ReamingEditBlock(doc);
            // Allow for plane rotation/translation with a gumball
            double planeSpan = 25;
            var xspan = new Interval(-planeSpan, planeSpan);
            var yspan = new Interval(-planeSpan, planeSpan);
            var gTransform = new GumballTransformPlane(doc, false);
            Transform planeTransform; // this will save the rotation/translation done to the plane
            reamingPlane = gTransform.TransformPlane(reamingPlane, xspan, yspan, out planeTransform);

            // Enlarge the reamingPlane for drawing of the curve
            var largeSpan = new Interval(-200, 200); // Oversize so user can resize along edges
            var reamingSurface = new PlaneSurface(reamingPlane, largeSpan, largeSpan);
            var reamingSurfaceId = doc.Objects.AddSurface(reamingSurface);
            doc.Views.Redraw();

            // Draw and extrude curve
            var ce = new CurveExtruder();
            Brep patchExtruded;
            ce.SetExistingCurveId(chosenEntityId);
            success = ce.ExtrudeCurve(doc, reamingSurface, planeTransform, out patchExtruded);
            doc.Objects.Delete(reamingSurfaceId, true);
            if (!success)
            {
                return Result.Failure;
            }

            // Add entity
            objManager.SetBuildingBlock(IBB.ExtraReamingEntity, patchExtruded, chosenEntityId);

            return Result.Success;
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, ImplantDirector director)
        {
            // Dependencies
            var dep = new Dependencies();
            dep.DeleteBlockDependencies(director, IBB.ExtraReamingEntity);
            dep.DeleteBlockDependencies(director, IBB.BoneGraft);
            dep.UpdateAdditionalReaming(director);
            // Set visibility
            Amace.Visualization.Visibility.ReamingDefault(doc);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, ImplantDirector director)
        {
            // Visualization
            Amace.Visualization.Visibility.ReamingDefault(doc);
        }
    }
}