using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI.Gumball;

namespace IDS.Core.Operations
{
    public class GumballTransformPlane : GumballTransform
    {
        public GumballTransformPlane(RhinoDoc doc, bool allowKeyboardEvents, 
            string commandPrompt = "Drag gumball. Press Enter when done.") : 
            base(doc, allowKeyboardEvents, commandPrompt)
        {
        }

        public Plane TransformPlane(Plane plane, Interval xspan, Interval yspan, out Transform transform)
        {
            // Gumball
            var gumball = new GumballObject();
            gumball.SetFromPlane(plane);
            var frame = gumball.Frame;
            frame.ScaleGripDistance = new Vector3d(20, 20, 20);
            frame.ScaleMode = GumballScaleMode.Independent;
            gumball.Frame = frame;
            var appearance = new GumballAppearanceSettings();
            appearance.ScaleXEnabled = true;
            appearance.ScaleYEnabled = true;
            appearance.ScaleZEnabled = false;
            appearance.ScaleGripSize = 8;

            var planeSurface = new PlaneSurface(plane, xspan, yspan);
            var planeId = ActiveDoc.Objects.AddSurface(planeSurface);
            var planeRef = new ObjRef(planeId);
            // Transform the plane
            transform = GumballTransformObject(planeRef, gumball, appearance);
            // Retrieve the plane
            var planeObject = ActiveDoc.Objects.Find(planeId);
            var planeBrep = (Brep)planeObject.Geometry;
            var newPlane = new Plane(planeBrep.Vertices[0].Location, planeBrep.Vertices[1].Location, planeBrep.Vertices[2].Location);
            // Remove solid plane
            ActiveDoc.Objects.Delete(planeId, true);

            return newPlane;
        }
    }
}
