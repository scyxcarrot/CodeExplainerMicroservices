using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.Core.Drawing
{
    public class DistanceConduit : DisplayConduit
    {
        private readonly Color color;
        private readonly Point3d center;
        private readonly double rotationFromCameraUp;
        private readonly double initialLineLength;

        private Line fromLine;
        private Line toLine;
        private Line centerLine;
        private readonly string textLabel;
        private Point3d textCoordinate;

        public DistanceConduit(Point3d ptFrom, Point3d ptTo, Color color, double value, double rotationFromCameraUp, double initialLineLength)
        {
            this.color = color;
            this.rotationFromCameraUp = rotationFromCameraUp;
            this.initialLineLength = initialLineLength;
            fromLine = new Line(ptFrom, ptTo);
            toLine = new Line(ptTo, ptFrom);
            textLabel = $"{value:F1}";

            var direction = ptTo - ptFrom;
            var length = direction.Length / 2;
            direction.Unitize();
            center = Point3d.Add(ptFrom, Vector3d.Multiply(direction, length));
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            var cameraUp = new Vector3d(e.Viewport.CameraUp);
            cameraUp.Rotate(RhinoMath.ToRadians(rotationFromCameraUp), e.Viewport.CameraDirection);
            var linesDirection = cameraUp;
            var centerVector = new Vector3d(linesDirection);
            centerVector.Rotate(RhinoMath.ToRadians(90), e.Viewport.CameraDirection);

            var visibleGeometries = new List<GeometryBase>();
            var settings = new ObjectEnumeratorSettings();
            settings.ActiveObjects = true;
            var rhobjs = e.RhinoDoc.Objects.FindByFilter(settings);
            foreach (var rhobj in rhobjs)
            {
                visibleGeometries.Add(rhobj.Geometry);
            }

            var length = initialLineLength;
            for (var i = 0; i <= 10; i++)
            {
                if (IsTextOverlapped(length, linesDirection, centerVector, e.Viewport.CameraDirection, visibleGeometries))
                {
                    length += 10.0;
                }
                else
                {
                    break;
                }
            }

            e.Display.EnableDepthWriting(false);
            e.Display.EnableDepthTesting(false);

            e.Display.DrawLine(fromLine, color);
            e.Display.DrawLine(toLine, color);
            e.Display.DrawLine(centerLine, color);

            e.Display.EnableDepthWriting(true);
            e.Display.EnableDepthTesting(true);
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            base.DrawForeground(e);

            var point = e.Viewport.WorldToClient(textCoordinate);
            var height = 20;
            var width = 50;
            var x = Convert.ToInt32(point.X) - (width / 2);
            var y = Convert.ToInt32(point.Y) - (height / 2);
            var rect = new Rectangle(x, y, width, height);
            e.Display.Draw2dRectangle(rect, color, 1, color);
            e.Display.Draw2dText(textLabel, Color.White, point, true);
        }

        private bool IsTextOverlapped(double length, Vector3d linesDirection, Vector3d centervector, Vector3d cameraDirection, List<GeometryBase> geometries)
        {
            var centerEnd = Point3d.Add(center, Vector3d.Multiply(linesDirection, length));
            centerLine = new Line(centerEnd, Point3d.Add(centerEnd, Vector3d.Multiply(centervector, length)));
            fromLine.To = Point3d.Add(fromLine.From, Vector3d.Multiply(linesDirection, length));
            toLine.To = Point3d.Add(toLine.From, Vector3d.Multiply(linesDirection, length));

            double a, b;
            if (Intersection.LineLine(centerLine, fromLine, out a, out b))
            {
                double c, d;
                if (Intersection.LineLine(centerLine, toLine, out c, out d))
                {
                    fromLine.To = fromLine.PointAt(b);
                    toLine.To = toLine.PointAt(d);
                    centerLine.From = fromLine.To;
                    centerLine.To = toLine.To;

                    var textDirection = centerLine.To - centerLine.From;
                    var halfLength = textDirection.Length / 2;
                    textDirection.Unitize();
                    textCoordinate = Point3d.Add(centerLine.From, Vector3d.Multiply(textDirection, halfLength));

                    var meshes = geometries.Where(geometry => geometry.ObjectType == ObjectType.Mesh).Select(geometry => geometry as Mesh);
                    var breps = geometries.Where(geometry => geometry.ObjectType == ObjectType.Brep).Select(geometry => geometry as Brep);
                    var meshPoints = Intersection.ProjectPointsToMeshes(meshes, new[] {textCoordinate}, cameraDirection, 10.0);
                    var brepPoints = Intersection.ProjectPointsToBreps(breps, new[] { textCoordinate }, cameraDirection, 10.0);
                    if (meshPoints.Length > 0 || brepPoints.Length > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
