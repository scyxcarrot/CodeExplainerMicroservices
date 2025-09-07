using Rhino.Display;
using Rhino.Geometry;

#if (STAGING)

namespace IDS.Core.NonProduction
{
    public class ArrowConduit : DisplayConduit
    {
        private Line _line;
        private readonly System.Drawing.Color _color;

        public ArrowConduit(Point3d ptFrom, Point3d ptTo, System.Drawing.Color color)
        {
            _line = new Line(ptFrom, ptTo);
            _color = color;
        }

        public ArrowConduit(Point3d ptStart, Vector3d vector, double length, System.Drawing.Color color)
        {
            var vecUnit = vector;
            vecUnit.Unitize();

            var ptTo = ptStart + Vector3d.Multiply(vecUnit, length);
            _line = new Line(ptStart, ptTo);
            _color = color;
        }

        public void SetLocation(Point3d ptStart, Vector3d vector, double length)
        {
            var vecUnit = vector;
            vecUnit.Unitize();

            var ptTo = ptStart + Vector3d.Multiply(vecUnit, length);
            _line.From = ptStart;
            _line.To = ptTo;
        }

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            base.PreDrawObjects(e);

            e.Display.DrawArrow(_line, _color);
        }

    }
}

#endif