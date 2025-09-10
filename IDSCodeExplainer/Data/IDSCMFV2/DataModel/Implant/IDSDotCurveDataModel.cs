using System;
using IDS.Interface.Geometry;
using IDS.Interface.Implant;
using System.Collections.Generic;

namespace IDS.CMF.V2.DataModel
{
    public class IDSDotCurveDataModel
    {
        public ICurve Curve { get; set; }
        public List<IDot> Dots { get; set; }
        public Guid Id { get; set; }
        public double ConnectionWidth { get; set; }
        public double ConnectionThickness { get; set; }
        public IVector3D AverageVector { get; set; }
    }
}
