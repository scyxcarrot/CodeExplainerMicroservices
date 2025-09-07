using IDS.Core.V2.Geometries;
using IDS.Interface.Geometry;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.RhinoInterfaces.Converter
{
    public static class RhinoCurveConverter
    {
        public static Curve ToRhinoPolyCurve(this ICurve curve)
        {
            return PolyCurve.CreateControlPointCurve(curve.Points.Select(
                RhinoPoint3dConverter.ToPoint3d));
        }

        public static IDSCurve ToIDSCurve(this Curve curve)
        {
            var polyCurve = curve.ToPolyline(0.01, 0.01, 0.001, 0.1);
            var points = new List<IPoint3D>();
            for (var i = 0; i < polyCurve.PointCount; i++)
            {
                points.Add(RhinoPoint3dConverter.ToIDSPoint3D(polyCurve.Point(i)));
            }

            return new IDSCurve(points);
        }

        public static IDSCurve ToIDSCurve(this Curve curve, int degree)
        {
            var fitCurve = curve.Fit(degree,0.01, 0.01).ToNurbsCurve();
            var points = fitCurve.Points
                .Select(point => new IDSPoint3D(point.X, point.Y, point.Z))
                .Cast<IPoint3D>()
                .ToList();

            return new IDSCurve(points);
        }

        public static ICurve ToICurve(this Curve curve)
        {
            return ToIDSCurve(curve);
        }
    }
}
