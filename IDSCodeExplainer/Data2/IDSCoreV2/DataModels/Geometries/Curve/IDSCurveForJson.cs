using IDS.Interface.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.V2.Geometries
{
    // Use JsonUtilities.Deserialize<IDSCurveForJson>(jsonCurve) for deserialization
    public class IDSCurveForJson
    {
        public List<IDSPoint3D> Points { get; set; }

        public ICurve GetICurve()
        {
            return new IDSCurve(Points.Cast<IPoint3D>().ToList());
        }
    }
}
