using IDS.Interface.Implant;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public class DotCurveDataModel
    {
        public Curve Curve { get; set; }
        public List<IDot> Dots { get; set; }
        public double ConnectionWidth { get; set; }
        public double ConnectionThickness { get; set; }
        public Vector3d AverageVector { get; set; }
    }
}
