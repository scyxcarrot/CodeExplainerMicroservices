using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;

namespace IDS.Core.Drawing
{
    public class LengthPreview
    {
        protected Point3d _fromPoint;
        public Point3d FromPoint
        {
            get { return _fromPoint; }
            set
            {
                _fromPoint = value;
                OnPointChanged();
            }
        }

        protected Point3d _toPoint;
        public Point3d ToPoint
        {
            get { return _toPoint; }
            set
            {
                _toPoint = value;
                OnPointChanged();
            }
        }

        public LengthPreview(Point3d fromPoint, Point3d toPoint)
        {
            _fromPoint = fromPoint;
            _toPoint = toPoint;
        }

        public virtual void DrawScrew(DisplayPipeline display)
        {
            display.DrawDot(100, 50, GetDisplayText(), Color.Black, Color.White);
        }

        protected virtual void OnPointChanged()
        {

        }

        protected virtual string GetDisplayText()
        {
            return $"{(_toPoint - _fromPoint).Length:F0}mm";
        }
    }
} 