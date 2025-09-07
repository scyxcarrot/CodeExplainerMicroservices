using Rhino.Geometry;

namespace IDS.Core.Utilities
{
    /// <summary>
    /// Delegate method for checking dynamic constraints
    /// </summary>
    /// <param name="curvePoint">A candicate interpolation point for the curve.</param>
    /// <returns>true if constraint satisfied, false otherwise</returns>
    public delegate bool CurvePointValidator(Point3d curvePoint);
}
