using System;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI.Gumball;

namespace IDS.Core.Operations
{
    public class GumballTransformBrep : GumballTransform
    {
        public GumballTransformBrep(RhinoDoc doc, bool allowKeyboardEvents, 
            string commandPrompt = "Drag gumball. Press Enter when done.") : 
            base(doc, allowKeyboardEvents, commandPrompt)
        {
        }

        public Transform TransformBrep(Guid brepId)
        {
            // Create mesh and calculate bounding box
            var brep = (Brep)ActiveDoc.Objects.Find(brepId).Geometry;
            var brepMeshes = Mesh.CreateFromBrep(brep);
            var brepMesh = new Mesh();
            foreach (var mesh in brepMeshes)
            {
                brepMesh.Append(mesh);
            }
            var bbox = brepMesh.GetBoundingBox(true);

            // Gumball
            var gumball = new GumballObject();
            // Initialize using bounding box of the brep
            gumball.SetFromBoundingBox(bbox);
            var gumballFrame = gumball.Frame;
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
            // Set the gumball origin at the centroid of the entity
            var vMassProp = VolumeMassProperties.Compute(brep);
            gumballPlane.Origin = vMassProp.Centroid;
            gumballFrame.Plane = gumballPlane;
            gumball.Frame = gumballFrame;
            var appearance = new GumballAppearanceSettings();
            appearance.ScaleXEnabled = true;
            appearance.ScaleYEnabled = true;
            appearance.ScaleZEnabled = true;
            appearance.ScaleGripSize = 8;

            // Get reference
            var brepRef = new ObjRef(brepId);
            // Transform
            return GumballTransformObject(brepRef, gumball, appearance);
        }
    }
}
