using IDS.CMF.DataModel;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.PICMF.Drawing
{
    public class DrawLimitSurfaceResult
    {
        public List<PatchData> InnerSurfaces { get; set; } = new List<PatchData>();
        public List<Point3d> ControlPoints { get; set; } = new List<Point3d>();
        public double ExtensionLength { get; set; } = 10;
    }
}