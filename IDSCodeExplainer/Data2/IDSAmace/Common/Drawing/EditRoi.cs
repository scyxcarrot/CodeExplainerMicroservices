using IDS.Amace.Visualization;
using IDS.Core.Drawing;
using Rhino;
using Rhino.Geometry;

namespace IDS.Common.Visualization
{
    public class EditRoi : DrawCurve
    {
        private readonly PlaneFacingCameraConduit _cameraConduit;

        public EditRoi(RhinoDoc document, Plane plane, double planeSize, Curve curve) : base(document)
        {
            AlwaysOnTop = true;
            var span = new Interval(-planeSize, planeSize);
            SetConstraintPlane(plane, span, false, false);
            SetExistingCurve(curve, true, false);

            OnPointListChanged += (changedPointList) =>
            {
                _cameraConduit.UpdatePlaneAndPoints(_constraintPlane, changedPointList);
            };

            _cameraConduit = new PlaneFacingCameraConduit(plane, PointList);
            _cameraConduit.OnCameraChanged += (changedPlane, changedPoints) =>
            {
                PointList = changedPoints;

                _constraintPlane = changedPlane;
                var surface = new PlaneSurface(changedPlane, span, span);
                _constraintSurface = surface;
                Constrain(_constraintSurface, false);
            };
        }

        public override Curve Draw(int maxPoints = 0)
        {
            _cameraConduit.Enabled = true;
            var newCurve =  base.Draw(maxPoints);
            _cameraConduit.Enabled = false;
            return newCurve;
        }

        public Plane GetConstraintPlane()
        {
            return new Plane(_constraintPlane);
        }
    }
}