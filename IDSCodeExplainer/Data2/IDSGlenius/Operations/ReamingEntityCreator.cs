using IDS.Core.Drawing;
using IDS.Core.Importer;
using IDS.Core.Operations;
using IDS.Glenius.FileSystem;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Glenius.Operations
{
    //Potential candidate to adapt aMace's implementation
    public class ReamingEntityCreator
    {
        private readonly RhinoDoc doc;

        //Delegates for dependency inversion
        public delegate void OnCurveDrawingPlaneCreated();
        public OnCurveDrawingPlaneCreated onCurveDrawingPlaneCreated;

        public delegate void OnReamingEntityCreated(bool success);
        public OnReamingEntityCreated onReamingEntityCreated;

        public ReamingEntityCreator(RhinoDoc doc)
        {
            this.doc = doc;
        }

        //If existingReamingEntity is set, it will use existing curve to edit. Else a brand new will be created. 
        private bool CreateReamingEntity(Plane reamingPlane, Guid existingReamingEntity, out Brep reamingEntity)
        {
            reamingEntity = null;

            // Allow for plane rotation/translation with a gumball
            double planeSpan = 25;
            Interval xspan = new Interval(-planeSpan, planeSpan);
            Interval yspan = new Interval(-planeSpan, planeSpan);
            var gTransform = new GumballTransformPlane(doc, false);
            Transform planeTransform; // this will save the rotation/translation done to the plane
            var newReamingPlane = gTransform.TransformPlane(reamingPlane, xspan, yspan, out planeTransform);

            // Enlarge the reamingPlane for drawing of the curve
            Interval largeSpan = new Interval(-200, 200); // Oversize so user can resize along edges
            Surface reamingSurface = new PlaneSurface(newReamingPlane, largeSpan, largeSpan);
            Guid reamingSurfaceId = doc.Objects.AddSurface(reamingSurface);
            doc.Views.Redraw();

            onCurveDrawingPlaneCreated?.Invoke();

            // Draw and extrude curve
            Brep patchExtruded;
            CurveExtruder ce = new CurveExtruder();
            ce.SetExistingCurveId(existingReamingEntity);
            var success = ce.ExtrudeCurve(doc, reamingSurface, planeTransform, out patchExtruded);

            doc.Objects.Delete(reamingSurfaceId, true);
            if (!success)
            {
                onReamingEntityCreated?.Invoke(success);
                return false;
            }

            reamingEntity = patchExtruded;
            onReamingEntityCreated?.Invoke(success);
            return true;
        }

        public bool CreateReamingEntity(Mesh constraintMesh, out Brep reamingEntity)
        {
            reamingEntity = null;
            Plane reamingPlane;

            PlaneDrawer pd = new PlaneDrawer();
            if (pd.ThreePointPlane(constraintMesh, out reamingPlane))
            {
                return CreateReamingEntity(reamingPlane, Guid.Empty, out reamingEntity);
            }
            else
            {
                return false;
            }
        }

        public bool CreateReamingEntityWithMimicsPlane(out Brep reamingEntity)
        {
            reamingEntity = null;
            Plane reamingPlane;

            if(PlaneImporter.ImportMimicsPlane(DirectoryStructure.GetWorkingDir(doc), out reamingPlane))
            {
                return CreateReamingEntity(reamingPlane, Guid.Empty, out reamingEntity);
            }

            return false;
        }

        public bool EditReamingEntity(out Brep reamingEntity, out Guid chosenEntityId)
        {
            reamingEntity = null;
            Plane reamingPlane;
            chosenEntityId = Guid.Empty;

            if (PlaneFromReamingEntity.GetPlane(doc, out chosenEntityId, out reamingPlane))
            {
                return CreateReamingEntity(reamingPlane, chosenEntityId, out reamingEntity);
            }

            return false;
        }

        public bool DoDeleteReamingEntity()
        {
            GetObject selectReamingBlocks = new GetObject();
            selectReamingBlocks.SetCommandPrompt("Select entities to remove.");
            selectReamingBlocks.EnablePreSelect(false, false);
            selectReamingBlocks.EnablePostSelect(true);
            selectReamingBlocks.AcceptNothing(true);
            selectReamingBlocks.EnableTransparentCommands(false);

            // Get user input
            while (true)
            {
                GetResult res = selectReamingBlocks.GetMultiple(0, 0);

                if (res == GetResult.Cancel || res == GetResult.Nothing)
                {
                    return false;
                }

                if (res == GetResult.Object)
                {
                    // Ask confirmation and delete if user clicks 'Yes'
                    DialogResult result = Rhino.UI.Dialogs.ShowMessageBox(
                        "Are you sure you want to delete the selected reaming entity / entities?",
                        "Delete Reaming Entities(s)?",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Exclamation);
                    if (result == DialogResult.Yes)
                    {
                        // Get selected objects
                        List<RhinoObject> selectedReamingBlocks = doc.Objects.GetSelectedObjects(false, false).ToList();
                        // Delete one by one (including dependencies)
                        foreach (RhinoObject rhobj in selectedReamingBlocks)
                        {
                            doc.Objects.Delete(rhobj.Id, true);
                        }

                        // Stop user input
                        break;
                    }
                    if (result == DialogResult.Cancel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool DoTransformReamingEntity()
        {
            GetObject selectReamingBlocks = new GetObject();
            selectReamingBlocks.SetCommandPrompt("Select reaming entity to transform.");
            selectReamingBlocks.EnablePreSelect(false, false);
            selectReamingBlocks.EnablePostSelect(true);
            selectReamingBlocks.AcceptNothing(true);
            selectReamingBlocks.EnableTransparentCommands(false);
            // Get user input
            GetResult res = selectReamingBlocks.Get();

            if ((res == GetResult.Nothing) || (res == GetResult.Cancel))
            {
                return false;
            }

            if (res == GetResult.Object)
            {
                // Get selected objects
                List<RhinoObject> selectedReamingBlocks = doc.Objects.GetSelectedObjects(false, false).ToList();

                // Transform object
                RhinoObject rhobj = selectedReamingBlocks[0];
                var gTransform = new GumballTransformBrep(doc, false);
                Transform objectTransform = gTransform.TransformBrep(rhobj.Id);
                if (objectTransform == Transform.Identity)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
