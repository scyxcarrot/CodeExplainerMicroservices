using IDS.Core.Drawing;
using Rhino.DocObjects;
using System.Collections.Generic;

namespace IDS.CMF.DataModel
{
    public class ImplantMarginInputGetterDataModel
    {
        public RhinoObject PlannedPartRhObject { get; set; }
        public RhinoObject OriginalPartRhObject { get; set; }
        public List<RhinoObject> OutlinesRhObject { get; set; }
    }

    public class ImplantMarginGetterDataModel
    {
        public ImplantMarginAttribute MarginAttribute { get; set; }
        public FullSphereConduit PointAConduit { get; set; }
        public FullSphereConduit PointBConduit { get; set; }
        public CurveConduit FullOutlineConduit { get; set; }
        public CurveConduit TrimmedCurveConduit { get; set; }
    }
}
