using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class EditLandmark
    {
        private readonly Point3d originalPoint;
        private readonly Mesh mesh;
        private readonly Stack<Point3d> selectedPoints;

        public Point3d Point { private set; get; }

        public EditLandmark(Point3d landmarkPoint, Mesh constraintMesh)
        {
            originalPoint = landmarkPoint;
            mesh = constraintMesh;
            selectedPoints = new Stack<Point3d>();
        }

        public Result Edit()
        {
            selectedPoints.Clear();
            Point = Point3d.Unset;
            return MoveToPoint();
        }

        private Result MoveToPoint()
        {
            var get = new GetPoint();
            get.SetCommandPrompt("Click on a point to edit");
            get.Constrain(mesh, false);
            get.FullFrameRedrawDuringGet = true;
            get.PostDrawObjects += PostDrawObjects;
            get.DynamicDraw += DynamicDraw;
            get.AcceptUndo(true);
            get.AcceptNothing(true); // accept ENTER to confirm
            get.EnableTransparentCommands(false);
            var cancelled = false;
            while (true)
            {
                var getRes = get.Get(); // function only returns after clicking
                if (getRes == GetResult.Cancel)
                {
                    cancelled = true;
                    break;
                }
                else if (getRes == GetResult.Point)
                {
                    var point = get.Point();
                    Point = point;
                    selectedPoints.Push(point);
                }
                else if (getRes == GetResult.Nothing)
                {
                    break;
                }
                else if (getRes == GetResult.Undo)
                {
                    var point = Point3d.Unset;
                    if (selectedPoints.Any())
                    {
                        selectedPoints.Pop();
                    }
                    if (selectedPoints.Any())
                    {
                        point = selectedPoints.Peek();
                    }
                    Point = point;
                }
                else { }
            }
            get.PostDrawObjects -= PostDrawObjects;
            get.DynamicDraw -= DynamicDraw;

            return cancelled ? Result.Cancel : Result.Success;
        }

        private void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            e.Display.DrawPoint(e.CurrentPoint, PointStyle.Simple, 10, Color.Blue);
        }

        private void PostDrawObjects(object sender, DrawEventArgs e)
        {
            e.Display.DrawPoint(originalPoint, PointStyle.Simple, 10, Color.Red);
            if (Point != Point3d.Unset)
            {
                e.Display.DrawPoint(Point, PointStyle.Simple, 10, Color.Green);
            }
        }
    }
}