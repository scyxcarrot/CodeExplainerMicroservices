using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Core.Utilities;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI.Gumball;

namespace IDS.Core.Operations
{
    public class GumballTranslateBrep : GumballTransform
    {
        public GumballTranslateBrep(RhinoDoc doc, bool allowKeyboardEvents, 
            string commandPrompt = "Drag gumball. Press Enter when done.") : 
            base(doc, allowKeyboardEvents, commandPrompt)
        {
        }

        public Transform TranslateBrep(Guid brepId, Point3d gumballCenter, Guid[] moveAlongBrepIds)
        {
            // Create mesh and calculate bounding box
            var brep = (Brep)ActiveDoc.Objects.Find(brepId).Geometry;
            var bbox = BrepUtilities.GetBoundingBoxFromMesh(brep);

            // Set gumball arrows parallel/perpendicular to a face of a brep that has a full loop (if available)
            Plane gumballPlane;
            if (brep.Loops.Count > 0)
            {
                gumballPlane = new Plane(brep.Loops[0].To3dCurve().PointAt(0), brep.Loops[0].To3dCurve().PointAt(0.3),
                    brep.Loops[0].To3dCurve().PointAt(0.6));
            }
            else
            {
                gumballPlane = Plane.WorldXY;
            }

            gumballPlane.Origin = gumballCenter;

            // Gumball
            var gumball = new GumballObject();

            var gumballFrame = gumball.Frame; //copy whatever it is previously there
            gumballFrame.Plane = gumballPlane;

            gumball.SetFromBoundingBox(bbox);
            gumball.Frame = gumballFrame;

            var appearance = new GumballAppearanceSettings
            {
                ScaleXEnabled = false,
                ScaleYEnabled = false,
                ScaleZEnabled = false,
                RotateXEnabled = false,
                RotateYEnabled = false,
                RotateZEnabled = false,
                PlanarTranslationGripSize = 40
            };

            ActiveDoc.Views.Redraw();

            // Get reference
            var brepRef = new ObjRef(brepId);
            var list = new List<ObjRef>();
            if (moveAlongBrepIds != null)
            {
                list.AddRange(moveAlongBrepIds.Select(id => new ObjRef(id)));
            }

            // Transform
            return GumballTransformObject(brepRef, gumball, appearance, list.ToArray());
        }
    }
}
