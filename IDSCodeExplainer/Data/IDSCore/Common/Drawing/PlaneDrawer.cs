using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace IDS.Core.Drawing
{
    public class PlaneDrawer
    {
        private readonly List<Point3d> _planePoints = new List<Point3d>();
        private PlaneConduit _conduit = null;

        public bool ThreePointPlane(Mesh constrainMesh, out Plane thePlane)
        {
            // init
            thePlane = new Plane();

            // conduit
            _conduit = new PlaneConduit();
            _conduit.Enabled = true;

            // Select three points
            GetPoint getPlanePoints = new GetPoint();
            getPlanePoints.SetCommandPrompt("Select three point to define a plane.");
            getPlanePoints.Constrain(constrainMesh, false);
            getPlanePoints.PermitObjectSnap(false);
            getPlanePoints.DynamicDraw += DrawThroughConduit;
            getPlanePoints.AcceptNothing(true); // accept ENTER to confirm
            getPlanePoints.EnableTransparentCommands(false);
            while (true)
            {
                GetResult getResult = getPlanePoints.Get(); // function only returns after clicking
                if (getResult == GetResult.Cancel)
                {
                    _conduit.Enabled = false;
                    return false;
                }
                else if (getResult == GetResult.Point)
                {
                    _planePoints.Add(getPlanePoints.Point());

                    if (_planePoints.Count == 3)
                        break;
                }
            }

            // Create the plane with its origin in the middle
            thePlane = new Plane(_planePoints[0], _planePoints[1], _planePoints[2]);
            thePlane.Origin = _planePoints[0] + (_planePoints[1] - _planePoints[0]) / 2 + (_planePoints[2] - _planePoints[0]) / 2;

            // Success
            _conduit.Enabled = false;
            return true;
        }

        private void DrawThroughConduit(Object sender, Rhino.Input.Custom.GetPointDrawEventArgs e)
        {
            _conduit.SetPoint(_planePoints.Count, e.CurrentPoint);
            // Trick conduit into redrawing
            e.Viewport.SetCameraLocations(e.Viewport.CameraTarget, e.Viewport.CameraLocation);
        }
    }
}