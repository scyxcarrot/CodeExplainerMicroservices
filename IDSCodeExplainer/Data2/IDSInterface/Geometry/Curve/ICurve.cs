using System.Collections.Generic;

namespace IDS.Interface.Geometry
{
    public interface ICurve
    {
        IList<IPoint3D> Points { get; }
    }
}
