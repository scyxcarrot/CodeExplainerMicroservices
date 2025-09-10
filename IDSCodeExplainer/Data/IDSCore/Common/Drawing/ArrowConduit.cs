using Rhino.Display;
using Rhino.Geometry;

namespace IDS.Core.Drawing
{
    public class ArrowConduit : DisplayConduit
    {
        private Line line;
        private readonly System.Drawing.Color color;

        public ArrowConduit(Point3d ptFrom, Point3d ptTo, System.Drawing.Color color)
        {
            line = new Line(ptFrom, ptTo);
            this.color = color;
        }

        public ArrowConduit(Point3d ptStart, Vector3d vector, double length, System.Drawing.Color color)
        {
            var vecUnit = vector;
            vecUnit.Unitize();

            var ptTo = ptStart + Vector3d.Multiply(vecUnit, length);
            line = new Line(ptStart, ptTo);
            this.color = color;
        }

        public void SetLocation(Point3d ptStart, Vector3d vector, double length)
        {
            var vecUnit = vector;
            vecUnit.Unitize();

            var ptTo = ptStart + Vector3d.Multiply(vecUnit, length);
            line.From = ptStart;
            line.To = ptTo;
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            e.Display.DrawArrow(line, color);
        }

    }
}
