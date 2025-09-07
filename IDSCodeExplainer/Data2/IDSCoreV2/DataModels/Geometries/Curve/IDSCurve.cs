using IDS.Interface.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.V2.Geometries
{
    public class IDSCurve : ICurve
    {
        private readonly List<IPoint3D> _points;

        public IDSCurve()
        {
            _points = new List<IPoint3D>();
        }

        public IDSCurve(List<IPoint3D> points): this()
        {
            _points = points.ToList();
        }

        public IDSCurve(ICurve source)
        {
            _points = source.Points.Select(p => (IPoint3D)new IDSPoint3D(p)).ToList();
        }

        public IList<IPoint3D> Points => _points;
    }
}
