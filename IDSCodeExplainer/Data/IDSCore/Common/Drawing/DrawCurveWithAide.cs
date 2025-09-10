using Rhino;
using Rhino.Geometry;
using Rhino.UI;

namespace IDS.Core.Drawing
{
    public class DrawCurveWithAide : DrawCurve
    {
        private readonly SphereConduit conduit;
       
        public DrawCurveWithAide(RhinoDoc doc, double sphereRadius) : base(doc)
        {
            conduit = new SphereConduit
            {
                Radius = sphereRadius
            };
        }
        
        protected override void OnKeyboard(int key)
        {
            if (!IsKeyDown(key))
            {
                return;
            }

            double radius;
            if (!TryGetRadius(key, out radius))
            {
                radius = conduit.Radius;
            }

            conduit.Radius = radius;
            LogCurrentRadius();

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.SetCameraLocations(RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraTarget, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation);
        }

        private bool TryGetRadius(int key, out double radius)
        {
            radius = -1.0;

            if (key >= 49 && key <= 57) //The 1-9 keys
            {
                radius = key - 48;
                return true;
            }
            else if (key >= 97 && key <= 105) //The 1-9 keys on the numeric keypad
            {
                radius = key - 96;
                return true;
            }

            return false;
        }

        public override Curve Draw(int maxPoints = 0)
        {
            conduit.Enabled = true;

            RhinoApp.KeyboardEvent += OnKeyboard;

            var mouseCallback = new IDSMouseCallback { Enabled = true };
            mouseCallback.MouseEnter += OnMouseEnter;
            mouseCallback.MouseLeave += OnMouseLeave;

            LogCurrentRadius();

            OnDynamicDrawing = OnDynamicDraw;

            var newCurve = base.Draw(maxPoints);

            RhinoApp.KeyboardEvent -= OnKeyboard;
            mouseCallback.Enabled = false;
            mouseCallback.MouseEnter -= OnMouseEnter;
            mouseCallback.MouseLeave -= OnMouseLeave;

            OnDynamicDrawing = null;

            conduit.Enabled = false;
            return newCurve;
        }

        private void LogCurrentRadius()
        {
            RhinoApp.WriteLine($"SphereRadius [Key 1-9]: {conduit.Radius}");
        }

        private void OnDynamicDraw(Point3d currentPoint)
        {
            if (existingCurve == null) //Create curve - placing control point
            {
                conduit.CenterPoint = currentPoint;

            }
            else if (_movingPointIndex != -1) //Edit curve - moving control point
            {
                conduit.CenterPoint = currentPoint;
                if (!conduit.Enabled)
                {
                    conduit.Enabled = true;
                }
            }
            else  //Edit curve - add/remove control point
            {
                conduit.Enabled = false;
            }
        }

        private void OnMouseEnter(MouseCallbackEventArgs e)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            RhinoApp.KeyboardEvent += OnKeyboard;
        }

        private void OnMouseLeave(MouseCallbackEventArgs e)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
        }
    }
}