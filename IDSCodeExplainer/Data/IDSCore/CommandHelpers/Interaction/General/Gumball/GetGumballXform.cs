using IDS.Core.Utilities;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI.Gumball;

namespace IDS.Core.Operations
{
    public class GetGumballTransform : GetTransform
    {
        private readonly RhinoDoc _doc;
        private readonly GumballDisplayConduit m_dc;

        private bool isRelocate = false;

        public GetGumballTransform(RhinoDoc doc, GumballDisplayConduit dc)
        {
            _doc = doc;
            m_dc = dc;
        }

        public override Transform CalculateTransform(Rhino.Display.RhinoViewport viewport, Point3d point)
        {
            return m_dc.InRelocate ? m_dc.PreTransform : m_dc.TotalTransform;
        }

        protected override void OnMouseDown(GetPointMouseEventArgs e)
        {
            if (m_dc.PickResult.Mode != GumballMode.None)
            {
                return;
            }
            m_dc.PickResult.SetToDefault();

            PickContext pick_context = new PickContext();
            pick_context.View = e.Viewport.ParentView;
            pick_context.PickStyle = PickStyle.PointPick;
            Transform xform = e.Viewport.GetPickTransform(e.WindowPoint);
            pick_context.SetPickTransform(xform);
            Rhino.Geometry.Line pick_line;
            e.Viewport.GetFrustumLine(e.WindowPoint.X, e.WindowPoint.Y, out pick_line);
            pick_context.PickLine = pick_line;
            pick_context.UpdateClippingPlanes();
            // pick gumball and, if hit, set getpoint dragging constraints.
            m_dc.PickGumball(pick_context, this);
        }

        protected override void OnMouseMove(GetPointMouseEventArgs e)
        {
            if (m_dc.PickResult.Mode == GumballMode.None)
            {
                return;
            }

            m_dc.CheckShiftAndControlKeys();
            Rhino.Geometry.Line world_line;
            if (!e.Viewport.GetFrustumLine(e.WindowPoint.X, e.WindowPoint.Y, out world_line))
            {
                world_line = Rhino.Geometry.Line.Unset;
            }

            bool rc = m_dc.UpdateGumball(e.Point, world_line);
            if (rc)
            {
                base.OnMouseMove(e);
            }
        }

        protected override void OnDynamicDraw(GetPointDrawEventArgs e)
        {

            foreach (var obj in ObjectList.ObjectArray())
            {
                var shadedBrep = (Brep)_doc.Objects.Find(obj.Id).DuplicateGeometry();
                shadedBrep.Transform(m_dc.PreTransform);
                if (isRelocate)
                {
                    // This is sort of a workaround due to Rhino's non-working "out-of-the-box" Gumball relocation
                    // Draw Shaded Brep to indicate the parts that are being transform during relocation
                    e.Display.DrawBrepShaded(shadedBrep, new DisplayMaterial());
                } 
                else if (m_dc.PickResult.Mode == GumballMode.None)
                {
                    // keeping the original brep invisible
                    RhinoObjectUtilities.SetRhObjVisibility(_doc, obj, false);
                    e.Display.DepthMode = DepthMode.Neutral;
                    e.Display.DrawBrepShaded(shadedBrep, new DisplayMaterial(obj.Attributes.ObjectColor));

                }
                else
                {
                    // Normal Transformation with gumball
                    RhinoObjectUtilities.SetRhObjVisibility(_doc, obj, true);
                }
            }
            RefreshConduit();
        }

        // lets user drag m_gumball around.
        public GetResult MoveGumball()
        {
            RhinoObjectUtilities.SetRhObjVisibility(_doc, ObjectList.ObjectArray(), true);
            RefreshConduit();

            isRelocate = false;
            // Get point on a MouseUp event
            if (m_dc.PreTransform != Transform.Identity)
            {
                HaveTransform = true;
                Transform = m_dc.PreTransform;
            }
            SetBasePoint(m_dc.BaseGumball.Frame.Plane.Origin, false);

            // V5 uses a display conduit to provide display feedback so shaded objects move
            ObjectList.DisplayFeedbackEnabled = true;

            if (Transform != Transform.Identity)
            {
                ObjectList.UpdateDisplayFeedbackTransform(Transform);
            }

            // Call Get with mouseUp set to true
            GetResult rc = this.Get(true);

            // V5 uses a display conduit to provide display feedback so shaded objects move
            ObjectList.DisplayFeedbackEnabled = false;
            return rc;
        }

        public GetResult RelocateGumball()
        {
            RhinoObjectUtilities.SetRhObjVisibility(_doc, ObjectList.ObjectArray(), false);
            RefreshConduit();

            isRelocate = true;
            SetBasePoint(m_dc.BaseGumball.Frame.Plane.Origin, false);
            ObjectList.DisplayFeedbackEnabled = false;

            // Call Get with mouseUp set to true
            GetResult rc = this.Get(true);
            return rc;
        }

        private void RefreshConduit()
        {
            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.
                SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget,
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
            RhinoDoc.ActiveDoc.Views.ActiveView.Redraw();
        }
    }
}