using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Input;
using Rhino;

namespace RhinoMtlsCommands.Utilities
{
    internal static class Getter
    {
        public static Guid GetCurve(string message, out Curve curve)
        {
            List<Curve> curves;
            var guids = GetCurves(message, 1, out curves);

            curve = curves[0];
            return guids[0];
        }

        public static List<Guid> GetCurves(string message, int maxAmount, out List<Curve> curves)
        {
            curves = new List<Curve>();
            var guids = new List<Guid>();

            var go = new GetObject
            {
                GeometryFilter = Rhino.DocObjects.ObjectType.Curve,
                SubObjectSelect = false,
                GroupSelect = false
            };
            go.AcceptNothing(true);
            go.SetCommandPrompt(message);
            go.GetMultiple(1, maxAmount);
            
            if (go.CommandResult() == Rhino.Commands.Result.Success)
            {
                foreach (var objRef in go.Objects())
                {
                    curves.Add(objRef.Curve());
                    guids.Add(objRef.ObjectId);
                }
            }

            return guids;
        }

        public static Guid GetMesh(string message, out Mesh mesh)
        {
            var go = new GetObject {GeometryFilter = Rhino.DocObjects.ObjectType.Mesh};
            go.DisablePreSelect();
            go.SubObjectSelect = false;
            go.GroupSelect = false;
            go.AcceptNothing(true);
            go.SetCommandPrompt(message);
            go.Get();
            var guid = Guid.Empty;
            if (go.CommandResult() == Rhino.Commands.Result.Success)
            {
                mesh = go.Object(0).Mesh();
                guid = go.Object(0).ObjectId;
            }
            else
            {
                mesh = null;
            }
            return guid;
        }

        public static Guid GetBrep(string message, out Brep brep)
        {
            var go = new GetObject { GeometryFilter = Rhino.DocObjects.ObjectType.Brep };
            go.DisablePreSelect();
            go.SubObjectSelect = false;
            go.GroupSelect = false;
            go.AcceptNothing(true);
            go.SetCommandPrompt(message);
            go.Get();
            var guid = Guid.Empty;
            if (go.CommandResult() == Rhino.Commands.Result.Success)
            {
                brep = go.Object(0).Brep();
                guid = go.Object(0).ObjectId;
            }
            else
            {
                brep = null;
            }
            return guid;
        }

        public static Point3d GetPoint3d(string message, Mesh meshConstraint)
        {
            var gp = new GetPoint();
            gp.PermitObjectSnap(false);
            gp.AcceptNothing(true); // accept ENTER to confirm
            gp.EnableTransparentCommands(false);
            gp.SetCommandPrompt(message);

            var res = gp.Get();
            if (res == GetResult.Point)
            {
                return GetPointOnConstraint(gp.Point(), meshConstraint, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraLocation,
                    RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection);
            }

            return Point3d.Unset;
        }

        public static Point3d GetPointOnConstraint(Point3d currentPoint, Mesh constraintPreview, Point3d cameraLocation, Vector3d cameraDirection)
        {
            var points = Intersection.ProjectPointsToMeshes(new List<Mesh> { constraintPreview }, new List<Point3d> { currentPoint }, cameraDirection, 0.0);
            if (points != null && points.Any())
            {
                //get the nearest point to camera
                var projectedPoint = points.OrderBy(point => point.DistanceTo(cameraLocation)).First();
                return projectedPoint;
            }
            return Point3d.Unset;
        }
    }
}
