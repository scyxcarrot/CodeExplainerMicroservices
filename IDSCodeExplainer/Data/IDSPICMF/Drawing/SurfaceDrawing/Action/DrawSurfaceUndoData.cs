using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.PICMF.Drawing
{
    public class DrawSurfaceUndoData
    {
        public List<Point3d> CurrentPointList { get; set; }
        public List<Point3d> AllPoints { get; set; }
        public int SelectedIndex { get; set; }
    }
}
