using Rhino.DocObjects;
using Rhino.Geometry;
using System;

namespace IDS.CMF.DataModel
{
    public class ImplantMarginAttribute
    {
        public Guid MarginGuid { get; set; }
        public RhinoObject MarginCurve { get; set; }
        public Curve MarginTrimmedCurve { get; set; }
        public Point3d PointA { get; set; }
        public Point3d PointB { get; set; }
        public double MarginThickness { get; set; }
        public RhinoObject OriginalPart { get; set; }
        public Curve OffsettedTrimmedCurve { get; set; }
    }
}
