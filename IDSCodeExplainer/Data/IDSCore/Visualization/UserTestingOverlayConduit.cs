using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;

namespace IDS.Core.Visualization
{
    public class UserTestingOverlayConduit : DisplayConduit
    {

        public static UserTestingOverlayConduit Instance { get; set; } = new UserTestingOverlayConduit();

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);
            e.Display.Draw2dText("WARNING!\nNot For Production Use!", Color.Red, new Point2d(10,30), false);
        }
    }
}
