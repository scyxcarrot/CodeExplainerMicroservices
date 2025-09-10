using IDS.Core.Utilities;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Drawing;

namespace IDS.CMF.Visualization
{
    public class GuideFixationScrewNumberBubbleConduit : NumberBubbleConduit
    {
        public Point3d FixedLocation { get; set; }
        public double AngleOffset { get; set; }
        public Circle OriginalBorder { get; set; }

        public GuideFixationScrewNumberBubbleConduit(Point3d location, int number, Color textColor, Color bubbleColor, double angleOffset) :
            base(location, number, textColor, bubbleColor)
        {
            AngleOffset = angleOffset;
            FixedLocation = location;
            Location = GetBubbleLocation();
        }

        private Point3d GetBubbleLocation()
        {
            var cameraLeftSideVec = -VectorUtilities.GetCameraRightSideVector();
            cameraLeftSideVec.Unitize();

            var tmpLoc = FixedLocation;
            var distanceOffset = BubbleRadius * 1.5;
            tmpLoc = tmpLoc + (cameraLeftSideVec * (-distanceOffset));

            var cameraDirection = RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection;
            cameraDirection.Unitize();

            var xform = Transform.Rotation(RhinoMath.ToRadians(AngleOffset), cameraDirection, FixedLocation);
            tmpLoc.Transform(xform);
            return tmpLoc;
        }

        protected override void DrawForeground(DrawEventArgs e)
        {
            if (Location == Point3d.Unset)
            {
                return;
            }

            Location = GetBubbleLocation();

            var txtEntity = new TextEntity();
            txtEntity.PlainText = "X";
            txtEntity.TextHorizontalAlignment = TextHorizontalAlignment.Center;
            txtEntity.TextVerticalAlignment = TextVerticalAlignment.Middle;
            txtEntity.TextHeight = DisplaySize;
            txtEntity.SetBold(true);

            RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.GetCameraFrame(out var camFrame);
            var xformTranslateNumber = Transform.Translation(FixedLocation - camFrame.Origin);
            var xformCamFrame = Transform.PlaneToPlane(Plane.WorldXY, camFrame);
            var xformFinalNumber = Transform.Multiply(xformTranslateNumber, xformCamFrame);

            e.Display.DrawText(txtEntity, BorderColor, xformFinalNumber);

            base.DrawForeground(e);
        }
    }
}
